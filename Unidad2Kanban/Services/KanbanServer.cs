using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Unidad2Kanban.Models;

namespace Unidad2Kanban.Services
{
    public class KanbanServer
    {
        HttpListener server = new();

        public event Action? TableroActualizado;

        private readonly Tablero _tablero = new();
        byte[]? index;

        public void Iniciar()
        {
            server.Prefixes.Add("http://*:20000/kanban/");
            server.Start();

            Thread hilo = new Thread(Escuchar)
            {
                IsBackground = true
            };
            hilo.Start();
        }

        void Escuchar()
        {
            var contexto = server.GetContext();
            new Thread(Escuchar) { IsBackground = true }.Start();

            if(contexto != null)
            {
                if(contexto.Request.HttpMethod == "GET" && (contexto.Request.RawUrl == "/kanban/" || contexto.Request.RawUrl == "/kanban/index"))
                {
                    if(index == null)
                    {
                        index = File.ReadAllBytes("assets/index.html");
                    }

                    contexto.Response.ContentLength64 = index.Length;
                    contexto.Response.ContentType = "text/html";
                    contexto.Response.OutputStream.Write(index, 0, index.Length);
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
                else if (contexto.Request.HttpMethod == "GET" && contexto.Request.RawUrl == "/kanban/tablero")
                {
                    string json = JsonSerializer.Serialize(_tablero);
                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    contexto.Response.ContentType = "application/json";
                    contexto.Response.ContentLength64 = buffer.Length;
                    contexto.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
                else if (contexto.Request.HttpMethod == "POST" && contexto.Request.RawUrl == "/kanban/agregar")
                {
                    byte[] bufferEntrada = new byte[contexto.Request.ContentLength64];
                    contexto.Request.InputStream.Read(bufferEntrada, 0, bufferEntrada.Length);
                    string json = Encoding.UTF8.GetString(bufferEntrada);
                    var tarea = JsonSerializer.Deserialize<Tarea>(json);
                    if (tarea != null)
                    {
                        _tablero.AgregarTarea(tarea);
                        TableroActualizado?.Invoke();
                    }
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
                else if (contexto.Request.HttpMethod == "POST" && contexto.Request.RawUrl == "/kanban/mover")
                {
                    byte[] bufferEntrada = new byte[contexto.Request.ContentLength64];
                    contexto.Request.InputStream.Read(bufferEntrada, 0, bufferEntrada.Length);
                    string json = Encoding.UTF8.GetString(bufferEntrada);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null && data.ContainsKey("id") && data.ContainsKey("estado"))
                    {
                        int id = int.Parse(data["id"]);
                        Estados nuevoEstado = Enum.Parse<Estados>(data["estado"]);
                        _tablero.MoverTarea(id, nuevoEstado);
                        TableroActualizado?.Invoke();
                    }
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
                else
                {
                    contexto.Response.StatusCode = 404;
                    contexto.Response.Close();
                }
            }
        }
    }
}

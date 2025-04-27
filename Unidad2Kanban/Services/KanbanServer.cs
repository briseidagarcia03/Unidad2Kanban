using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Unidad2Kanban.Models;

namespace Unidad2Kanban.Services
{
    public class KanbanServer
    {
        HttpListener server = new();

        public event Action? TableroActualizado;

        //checa esto
        private readonly string archivoTareas = "tareas.json";

        private readonly Tablero _tablero = new();
        byte[]? index;

        public KanbanServer()
        {

            _tablero = CargarTablero();
        }

        //checar esto
        private void GuardarTablero()
        {
            var opciones = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            File.WriteAllText(archivoTareas, JsonSerializer.Serialize(_tablero, opciones));
        }

        private Tablero CargarTablero()
        {
            try
            {
                if (File.Exists(archivoTareas))
                {
                    var opciones = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    };
                    string json = File.ReadAllText(archivoTareas);
                    return JsonSerializer.Deserialize<Tablero>(json, opciones) ?? new Tablero();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cargar tablero: " + ex.Message);
            }
            return new Tablero();
        }
        

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
                    var opciones = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    };

                    string json = JsonSerializer.Serialize(_tablero, opciones);

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

                    Console.WriteLine("JSON recibido: " + json);

                    var tarea = JsonSerializer.Deserialize<Tarea>(json);
                    if (tarea != null)
                    {
                        tarea.Estado = Estados.Pendiente;

                        tarea.Id = _tablero.Tareas.Count > 0 ? _tablero.Tareas.Max(t => t.Id) + 1 : 1;
                        _tablero.Tareas.Add(tarea);
                        //checaaaaaaar
                        GuardarTablero();
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
                        var tarea = _tablero.Tareas.FirstOrDefault(t => t.Id == id);
                        if (tarea != null)
                        {
                            tarea.Estado = nuevoEstado;
                            GuardarTablero();
                            TableroActualizado?.Invoke();
                        }
                  
                    }
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
                //estoy viendo esto aún
                else if (contexto.Request.HttpMethod == "POST" && contexto.Request.RawUrl == "/kanban/eliminar")
                {
                    byte[] bufferEntrada = new byte[contexto.Request.ContentLength64];
                    contexto.Request.InputStream.Read(bufferEntrada, 0, bufferEntrada.Length);
                    string json = Encoding.UTF8.GetString(bufferEntrada);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null && data.ContainsKey("id"))
                    {
                        int id = int.Parse(data["id"]);
                        var tarea = _tablero.Tareas.FirstOrDefault(t => t.Id == id);
                        if (tarea != null)
                        {
                            _tablero.Tareas.Remove(tarea);
                            GuardarTablero();
                            TableroActualizado?.Invoke();
                        }
                    }

                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
                else if (contexto.Request.HttpMethod == "POST" && contexto.Request.RawUrl == "/kanban/editar")
                {
                    byte[] bufferEntrada = new byte[contexto.Request.ContentLength64];
                    contexto.Request.InputStream.Read(bufferEntrada, 0, bufferEntrada.Length);
                    string json = Encoding.UTF8.GetString(bufferEntrada);

                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null && data.ContainsKey("id") && data.ContainsKey("titulo") && data.ContainsKey("descripcion"))
                    {
                        int id = int.Parse(data["id"]);
                        string titulo = data["titulo"];
                        string descripcion = data["descripcion"];

                        var tarea = _tablero.Tareas.FirstOrDefault(t => t.Id == id);
                        if (tarea != null)
                        {
                            tarea.Titulo = titulo;
                            tarea.Descripcion = descripcion;
                            GuardarTablero();
                            TableroActualizado?.Invoke();
                        }
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

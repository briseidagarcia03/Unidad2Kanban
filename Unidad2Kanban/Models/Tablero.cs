using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unidad2Kanban.Models
{
    public class Tablero
    {
        public List<Tarea> Tareas { get; set; } = new();

        public void AgregarTarea(Tarea tarea)
        {
            tarea.Id = Tareas.Count > 0 ? Tareas.Max(t => t.Id) + 1 : 1;
            Tareas.Add(tarea);
        }

        public void MoverTarea(int id, Estados nuevo)
        {
            var tarea = Tareas.FirstOrDefault(t => t.Id == id);
            if(tarea != null)
            {
                tarea.Estado = nuevo;
            }
        }
    }
}

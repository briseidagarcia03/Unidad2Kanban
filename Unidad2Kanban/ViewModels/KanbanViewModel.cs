using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unidad2Kanban.Services;

namespace Unidad2Kanban.ViewModels
{
    public class KanbanViewModel
    {
        KanbanServer server = new();

        public KanbanViewModel()
        {
            server.Iniciar();
        }
    }
}

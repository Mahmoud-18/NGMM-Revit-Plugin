using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RevitProject
{
    public class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public Action<object> Excute { get; set; }
        public Predicate<object> CanExcute { get; set; }

        public Command(Action<object> excute, Predicate<Object> canExcute = null)

        {
            Excute = excute;

            CanExcute = canExcute;
        }


        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Excute(parameter);
        }
    }
}

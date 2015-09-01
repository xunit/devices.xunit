using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Xunit.Runners
{
    public class DelegateCommand : ICommand
    {
        readonly Action execute;
        readonly Func<bool> canExecute;
        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action execute, Func<bool> canexecute = null)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            this.execute = execute;
            canExecute = canexecute ?? (() => true);
        }

        [DebuggerStepThrough]
        public bool CanExecute(object p)
        {
            try { return canExecute(); }
            catch { return false; }
        }

        public void Execute(object p)
        {
            if (!CanExecute(p))
                return;
            execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }


    public class DelegateCommand<T> : ICommand
    {
        readonly Action<T> execute;
        readonly Func<T, bool> canExecute;
        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<T> execute, Func<T, bool> canexecute = null)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            this.execute = execute;
            canExecute = canexecute ?? (e => true);
        }

        [DebuggerStepThrough]
        public bool CanExecute(object p)
        {
            try
            {
                if (p != null && !(p is T))
                {
                    p = (T)Convert.ChangeType(p, typeof(T));
                }
                return canExecute?.Invoke((T)p) ?? true;
            }
            catch { return false; }
        }

        public void Execute(object p)
        {
            if (!CanExecute(p))
                return;

            if (p != null && !(p is T))
            {
                p = (T)Convert.ChangeType(p, typeof(T));
            }
            execute((T)p);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }


}


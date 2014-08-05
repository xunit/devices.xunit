using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Xunit.Runners
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool Set<T>(ref T destination, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(destination, value))
            {
                destination = value;

                RaisePropertyChanged(propertyName);

                return true;
            }
            return false;
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var evt = PropertyChanged;
            if (evt != null)
            {
                evt(this, new PropertyChangedEventArgs(propertyName));
            }

        }
    }
}

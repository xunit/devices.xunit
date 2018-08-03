using System;
using System.Windows.Input;
using Xamarin.Forms;

namespace Xunit.Runners.Utilities
{
    public class CommandViewCell : ViewCell
    {

        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(CommandViewCell), default(ICommand),
                  propertyChanging: (bindable, oldvalue, newvalue) =>
                  {
                      var commandViewCell = (CommandViewCell)bindable;
                      var oldcommand = (ICommand)oldvalue;
                      if (oldcommand != null)
                          oldcommand.CanExecuteChanged -= commandViewCell.OnCommandCanExecuteChanged;
                  }, propertyChanged: (bindable, oldvalue, newvalue) =>
                  {
                      var commandViewCell = (CommandViewCell)bindable;
                      var newcommand = (ICommand)newvalue;
                      if (newcommand != null)
                      {
                          commandViewCell.IsEnabled = newcommand.CanExecute(commandViewCell.CommandParameter);
                          newcommand.CanExecuteChanged += commandViewCell.OnCommandCanExecuteChanged;
                      }
                  });

        public static readonly BindableProperty CommandParameterProperty = 
            BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(CommandViewCell), default(object),
                   propertyChanged: (bindable, oldvalue, newvalue) =>
                   {
                       var commandViewCell = (CommandViewCell)bindable;
                       if (commandViewCell.Command != null)
                       {
                           commandViewCell.IsEnabled = commandViewCell.Command.CanExecute(newvalue);
                       }
                   });

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }


        public CommandViewCell()
        {
        }

        protected override void OnTapped()
        {
            base.OnTapped();


            if (!IsEnabled)
            {
                return;
            }

            Command?.Execute(CommandParameter);
        }

        void OnCommandCanExecuteChanged(object sender, EventArgs eventArgs)
        {
            IsEnabled = Command.CanExecute(CommandParameter);
        }
    }
}

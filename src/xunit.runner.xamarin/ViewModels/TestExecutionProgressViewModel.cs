using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Xamarin.Forms;

namespace Xunit.Runners.ViewModels
{
    public sealed class TestExecutionProgressViewModel : ViewModelBase, IDisposable
    {
        private readonly IList<TestViewModel> tests;
        private int failedTestCount;
        private int passedTestCount;
        private int skippedTestCount;
        private double progress;

        public TestExecutionProgressViewModel(IEnumerable<TestViewModel> tests)
        {
            this.tests = tests.ToList();

            this.AttachToTests();
        }

        public int TestCount
        {
            get { return this.tests.Count; }
        }

        public int FailedTestCount
        {
            get { return this.failedTestCount; }
            private set
            {
                if (this.Set(ref this.failedTestCount, value))
                {
                    this.RaisePropertyChanged("CompletedTestCount");
                    this.RaisePropertyChanged("Progress");
                    this.RaisePropertyChanged("EffectiveColor");
                }
            }
        }

        public int SkippedTestCount
        {
            get { return this.skippedTestCount; }
            private set
            {
                if (this.Set(ref this.skippedTestCount, value))
                {
                    this.RaisePropertyChanged("CompletedTestCount");
                    this.RaisePropertyChanged("Progress");
                    this.RaisePropertyChanged("EffectiveColor");
                }
            }
        }

        public int PassedTestCount
        {
            get { return this.passedTestCount; }
            private set
            {
                if (this.Set(ref this.passedTestCount, value))
                {
                    this.RaisePropertyChanged("PassedTestCount");
                    this.RaisePropertyChanged("Progress");
                    this.RaisePropertyChanged("EffectiveColor");
                }
            }
        }

        public int CompletedTestCount
        {
            get { return this.FailedTestCount + this.SkippedTestCount + this.PassedTestCount; }
        }

        public double Progress
        {
            get { return (double)this.CompletedTestCount / this.TestCount; }
        }

        public Color EffectiveColor
        {
            get
            {
                if (this.FailedTestCount > 0)
                {
                    return Colors.Failure;
                }
                else if (this.SkippedTestCount > 0)
                {
                    return Colors.RunningWithSkipped;
                }

                return Colors.Success;
            }
        }

        private void AttachToTests()
        {
            foreach (var test in this.tests)
            {
                this.AttachToTest(test);
            }
        }

        private void AttachToTest(TestViewModel test)
        {
            test.PropertyChanged += this.TestPropertyChanged;
        }

        private void DetachFromTests()
        {
            foreach (var test in this.tests)
            {
                this.DetachFromTest(test);
            }
        }

        private void DetachFromTest(TestViewModel test)
        {
            test.PropertyChanged -= this.TestPropertyChanged;
        }

        private void TestPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var test = (TestViewModel)sender;


            switch (e.PropertyName)
            {
                case "IsPassed":
                    if (test.IsPassed)
                    {
                        this.PassedTestCount += 1;
                    }
                    break;
                case "IsFailed":
                    if (test.IsFailed)
                    {
                        this.FailedTestCount += 1;
                    }
                    break;
                case "IsSkipped":
                    if (test.IsSkipped)
                    {
                        this.SkippedTestCount += 1;
                    }
                    break;
            }
        }

        public void Dispose()
        {
            this.DetachFromTests();
        }
    }
}
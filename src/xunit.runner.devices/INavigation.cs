using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xunit.Runners
{
    /// <summary>
    /// 
    /// </summary>
    public interface INavigation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="dataContext"></param>
        /// <returns></returns>
        Task NavigateTo(NavigationPage page, object dataContext = null);
    }

    /// <summary>
    /// 
    /// </summary>
    public enum NavigationPage
    {
        Home,
        AssemblyTestList,
        TestResult,
        Credits
    }
}
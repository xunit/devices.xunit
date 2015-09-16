using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Xunit;



public class UWPRunnerTests
{
    [Fact]
    public void TestDiscovered()
    {
        Assert.True(true);
    }

    [Fact]
    public void FailingTest()
    {
        Assert.True(false);
    }

    [Fact]
    public async Task TestAsync()
    {
        await Task.Delay(1000);

        Assert.True(true);
    }

    [UIFact]
    public async void DoSomethingOnUIThread()
    {
        var ele = new SearchBox();
        ele.Visibility = Visibility.Collapsed;

        ele.QueryText = "foo";

        Assert.Equal("foo", ele.QueryText);

        await Task.Delay(20);

        ele.QueryText = "bar";
        Assert.Equal("bar", ele.QueryText);
    }

    [UITheory]
    [InlineData("foo")]
    [InlineData("FooBar")]
    [InlineData("Bar")]
    public void UITheory(string value)
    {
        var ele = new SearchBox();
        ele.Visibility = Visibility.Collapsed;
        ele.QueryText = value;


        Assert.Equal(3, ele.QueryText.Length);
    }
    

    [Fact]
    public async void TestAsyncVoid()
    {
        var i = 0;
        await Task.Delay(500);

        i += 10;

        Assert.Equal(10, i);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TestTheory(int i)
    {
        // Will fail twice
        Assert.Equal(0, i % 2);
    }

    [Fact(Skip ="not run")]
    public void SkippedTest()
    {
            
    }
}

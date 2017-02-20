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
    public void FailingTestWithVeryLongNameThatKeepsGoing()
    {
        Assert.True(false);
    }

    [Fact]
    public async Task TestAsync()
    {
        await Task.Delay(1000);

        Assert.True(true);
    }

    [Fact]
    public async void TestAsyncVoid()
    {
        var i = 0;
        await Task.Delay(500);

        i += 10;

        Assert.Equal(10, i);
    }

    [Fact]
    public async void TestLongMethod()
    {
        await Task.Delay(15000);
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

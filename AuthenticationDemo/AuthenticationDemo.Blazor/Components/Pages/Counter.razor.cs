using Microsoft.AspNetCore.Components;

namespace AuthenticationDemo.Blazor.Components.Pages
{
    public partial class Counter : ComponentBase
    {
        protected int currentCount = 0;

        protected void IncrementCount()
        {
            currentCount++;
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bkl.Infrastructure
{
    public  interface ILibBootstrap
    {
        void FromService(IServiceCollection serivce);
        Type GetMainServiceType();
    }
}

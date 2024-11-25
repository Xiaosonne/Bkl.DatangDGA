using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bkl.Dst.Interfaces
{

    public interface IHttpGain : IGrainWithStringKey
    {
        Task Weakup();
    }
}

#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Model
{
    public interface IOnboardMgr
    {
        bool Completed { get; }

        Task<Dictionary<string, OnboardResponse>> DownloadAsync();

        Dictionary<string, OnboardResponse> GetStatus();

        Task ResetAsync();

        OnboardResponse SelectTable(string lanternId);

        Task UploadAsync(Dictionary<string, OnboardResponse> config);
    }
}

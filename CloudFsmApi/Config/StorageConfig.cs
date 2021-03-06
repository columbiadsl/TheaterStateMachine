﻿#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Microsoft.Extensions.Options;

namespace CloudFsmApi.Config
{
    public class StorageConfig : IOptions<StorageConfig>
    {
        public string CharacterFilePath { get; set; }
        public string LanternToCharacterMapFilePath { get; set; }
        public string SceneFilePath { get; set; }
        public string StorageCnxnString { get; set; }

        public StorageConfig Value => this;
    }
}

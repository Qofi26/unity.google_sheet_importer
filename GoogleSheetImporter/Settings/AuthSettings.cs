using System;
using UnityEngine;

namespace GoogleSheetImporter.Settings
{
    [Serializable]
    internal sealed class AuthSettings : IAuthSettings
    {
        [field: SerializeField] public AuthMode AuthMode { get; set; }
        [field: SerializeField] public string ClientId { get; set; }
        [field: SerializeField] public string ClientSecret { get; set; }
        [field: SerializeField] public string ServiceAccountKeyPath { get; set; }
    }
}

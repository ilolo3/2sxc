﻿using System;

namespace ToSic.SexyContent.Interfaces
{
    /// <summary>
    /// Basic interface which tells the system that this item has editing-information attached
    /// </summary>
    internal interface IHasEditingData
    {
        int SortOrder { get; set; }
        DateTime ContentGroupItemModified { get; set; }
    }
}

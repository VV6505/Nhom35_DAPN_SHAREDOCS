using System;
using System.Collections.Generic;

namespace HeThong_User.Models;

public partial class DoQuy
{
    public string MaDq { get; set; } = null!;

    public int? MucDoQuy { get; set; }

    public int? DiemTl { get; set; }
}

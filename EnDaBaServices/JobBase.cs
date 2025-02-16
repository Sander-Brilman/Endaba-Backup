using System;

namespace EnDaBaServices;

public record JobBase
{
    public int Attempts { get; set; } = -1; 
}

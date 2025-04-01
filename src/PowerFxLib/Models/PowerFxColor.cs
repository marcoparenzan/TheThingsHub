using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PowerFxLib.Models;

public struct PowerFxColor
{
    static PowerFxColor Transparent = new PowerFxColor {
        R = 0,
        G = 0,
        B = 0, 
        A = 255
    };

    static Regex regexColorString = new Regex(@"\s*\[\s*(?<red>\d+)\s*\,\s*(?<green>\d+)\s*\,\s*(?<blue>\d+)\s*(?<alpha>\d+)?\s*\]\s*");
    public static bool TryParse(string input, out PowerFxColor value)
    {
        value = Transparent;
        var match = regexColorString.Match(input);
        if (!match.Success) return false;
        var r = int.Parse(match.Groups["red"].Value);
        var g = int.Parse(match.Groups["green"].Value);
        var b = int.Parse(match.Groups["blue"].Value);
        var a = int.Parse(match.Groups["alpha"].Value ?? "0");
        value = From(r,g,b,a);
        return true;
    }

    public static implicit operator PowerFxColor((int r, int g, int b) value) => From(value);
    public static implicit operator PowerFxColor((int r, int g, int b, int a) value) => From(value);

    public static PowerFxColor From((int r, int g, int b) value) => new() {
        R = value.r,
        G = value.g,
        B = value.b,
        A = 0
    };
    public static PowerFxColor From((int r, int g, int b, int a) value) => new()
    {
        R = value.r,
        G = value.g,
        B = value.b,
        A = value.a
    };

    public static PowerFxColor From(int r, int g, int b, int a = 0) => new()
    {
        R = r,
        G = g,
        B = b,
        A = a
    };

    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    public int A { get; set; }
}

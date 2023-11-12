using System;
using System.Collections.Generic;

namespace DeliveryOrderAPI.Models;

public partial class Employee
{
    public string Code { get; set; } = null!;

    public string? Pren { get; set; }

    public string? Name { get; set; }

    public string? Surn { get; set; }

    public string? Sex { get; set; }

    public string? Tpren { get; set; }

    public string? Tname { get; set; }

    public string? Tsurn { get; set; }

    public string? Tsex { get; set; }

    public DateOnly? Birth { get; set; }

    public DateOnly? Join { get; set; }

    public DateOnly? Resign { get; set; }

    public string? Religion { get; set; }

    public string? Rstype { get; set; }

    public string? Dvcd { get; set; }

    public string? Wtype { get; set; }

    public string? Wsts { get; set; }

    public string? Posit { get; set; }

    public string? PRank { get; set; }

    public string? PGrade { get; set; }

    public string? Bus { get; set; }

    public string? Stop { get; set; }

    public string? Tcaddr3 { get; set; }

    public string? Tcaddr4 { get; set; }

    public string? Tctel { get; set; }

    public string? Telephone { get; set; }

    public string? Mail { get; set; }

    public string? Otype { get; set; }

    public string? Dlrate { get; set; }

    public string? Grpot { get; set; }

    public string? Nickname { get; set; }

    public string? Costcenter { get; set; }

    public string? Company { get; set; }

    public string? Tposiname { get; set; }

    public DateOnly? TposijoinDt { get; set; }

    public DateOnly? AnnualcalDt { get; set; }

    public string? Andon { get; set; }

    public string? Location { get; set; }

    public string? Workcenter { get; set; }

    public string? Budgettype { get; set; }

    public string? LineCode { get; set; }
}

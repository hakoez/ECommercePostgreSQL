using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using InventoryManagment;

public class Customer : User
{
    public List<Order> Orders { get; set; } = new List<Order>();
}

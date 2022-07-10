
using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Xunit;

namespace Serde.Xml.Test;

/* The XmlRootAttribute allows you to set an alternate name
   (PurchaseOrder) of the XML element, the element namespace; by
   default, the XmlSerializer uses the class name. The attribute
   also allows you to set the XML namespace for the element.  Lastly,
   the attribute sets the IsNullable property, which specifies whether
   the xsi:null attribute appears if the class instance is set to
   a null reference. */
[GenerateSerialize]
[XmlRootAttribute("PurchaseOrder", Namespace="http://www.cpandl.com", IsNullable = false)]
public partial class PurchaseOrder
{
   public Address ShipTo;
   public string OrderDate;
   /* The XmlArrayAttribute changes the XML element name
    from the default of "OrderedItems" to "Items". */
   [SerdeMemberOptions(Rename = "Items")]
   public OrderedItem[] OrderedItems;
   public decimal SubTotal;
   public decimal ShipCost;
   public decimal TotalCost;
}

[GenerateSerialize]
public partial class Address
{
   /* The XmlAttribute instructs the XmlSerializer to serialize the Name
      field as an XML attribute instead of an XML element (the default
      behavior). */
   [XmlAttribute]
   [SerdeMemberOptions(ProvideAttributes = true)]
   public string Name;
   public string Line1;

   /* Setting the IsNullable property to false instructs the
      XmlSerializer that the XML attribute will not appear if
      the City field is set to a null reference. */
   [XmlElementAttribute(IsNullable = false)]
   public string City;
   public string State;
   public string Zip;
}

[GenerateSerialize]
public partial class OrderedItem
{
   public string ItemName;
   public string Description;
   public decimal UnitPrice;
   public int Quantity;
   public decimal LineTotal;

   /* Calculate is a custom method that calculates the price per item,
      and stores the value in a field. */
   public void Calculate()
   {
      LineTotal = UnitPrice * Quantity;
   }
}

public class SampleTest
{
   private const string Expected = """
<?xml version="1.0" encoding="utf-8"?>
<PurchaseOrder xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns="http://www.cpandl.com">
    <ShipTo Name="Teresa Atkinson">
        <Line1>1 Main St.</Line1>
        <City>AnyTown</City>
        <State>WA</State>
        <Zip>00000</Zip>
    </ShipTo>
    <OrderDate>Wednesday, June 27, 2001</OrderDate>
    <Items>
        <OrderedItem>
            <ItemName>Widget S</ItemName>
            <Description>Small widget</Description>
            <UnitPrice>5.23</UnitPrice>
            <Quantity>3</Quantity>
            <LineTotal>15.69</LineTotal>
        </OrderedItem>
    </Items>
    <SubTotal>15.69</SubTotal>
    <ShipCost>12.51</ShipCost>
    <TotalCost>28.2</TotalCost>
</PurchaseOrder>
""";
   private const string ExpectedFixed = """
<PurchaseOrder>
  <ShipTo>
    <Name>Teresa Atkinson</Name>
    <Line1>1 Main St.</Line1>
    <City>AnyTown</City>
    <State>WA</State>
    <Zip>00000</Zip>
  </ShipTo>
  <OrderDate>Thursday, January 1, 1970</OrderDate>
  <OrderedItems>
    <OrderedItem>
      <ItemName>Widget S</ItemName>
      <Description>Small widget</Description>
      <UnitPrice>5.23</UnitPrice>
      <Quantity>3</Quantity>
      <LineTotal>15.69</LineTotal>
    </OrderedItem>
  </OrderedItems>
  <SubTotal>15.69</SubTotal>
  <ShipCost>12.51</ShipCost>
  <TotalCost>28.20</TotalCost>
</PurchaseOrder>
""";

   [Fact]
   public void VerifySerialize()
   {
      // Create an instance of the XmlSerializer class;
      // specify the type of object to serialize.
      PurchaseOrder po = new PurchaseOrder();

      // Create an address to ship and bill to.
      Address billAddress = new Address();
      billAddress.Name = "Teresa Atkinson";
      billAddress.Line1 = "1 Main St.";
      billAddress.City = "AnyTown";
      billAddress.State = "WA";
      billAddress.Zip = "00000";
      // Set ShipTo and BillTo to the same addressee.
      po.ShipTo = billAddress;
      po.OrderDate = System.DateTime.UnixEpoch.ToLongDateString();

      // Create an OrderedItem object.
      OrderedItem i1 = new OrderedItem();
      i1.ItemName = "Widget S";
      i1.Description = "Small widget";
      i1.UnitPrice = (decimal) 5.23;
      i1.Quantity = 3;
      i1.Calculate();

      // Insert the item into the array.
      OrderedItem [] items = {i1};
      po.OrderedItems = items;
      // Calculate the total cost.
      decimal subTotal = new decimal();
      foreach(OrderedItem oi in items)
      {
         subTotal += oi.LineTotal;
      }
      po.SubTotal = subTotal;
      po.ShipCost = (decimal) 12.51;
      po.TotalCost = po.SubTotal + po.ShipCost;

      // Serialize the purchase order, and close the TextWriter.
      var actual = XmlSerializer.Serialize(po);
      Assert.Equal(ExpectedFixed, actual);
   }


//   protected void ReadPO(string filename)
//   {
//      // Create an instance of the XmlSerializer class;
//      // specify the type of object to be deserialized.
//      XmlSerializer serializer = new XmlSerializer(typeof(PurchaseOrder));
//      /* If the XML document has been altered with unknown
//      nodes or attributes, handle them with the
//      UnknownNode and UnknownAttribute events.*/
//      serializer.UnknownNode+= new
//      XmlNodeEventHandler(serializer_UnknownNode);
//      serializer.UnknownAttribute+= new
//      XmlAttributeEventHandler(serializer_UnknownAttribute);
//
//      // A FileStream is needed to read the XML document.
//      FileStream fs = new FileStream(filename, FileMode.Open);
//      // Declare an object variable of the type to be deserialized.
//      PurchaseOrder po;
//      /* Use the Deserialize method to restore the object's state with
//      data from the XML document. */
//      po = (PurchaseOrder) serializer.Deserialize(fs);
//      // Read the order date.
//      Console.WriteLine ("OrderDate: " + po.OrderDate);
//
//      // Read the shipping address.
//      Address shipTo = po.ShipTo;
//      ReadAddress(shipTo, "Ship To:");
//      // Read the list of ordered items.
//      OrderedItem [] items = po.OrderedItems;
//      Console.WriteLine("Items to be shipped:");
//      foreach(OrderedItem oi in items)
//      {
//         Console.WriteLine("\t"+
//         oi.ItemName + "\t" +
//         oi.Description + "\t" +
//         oi.UnitPrice + "\t" +
//         oi.Quantity + "\t" +
//         oi.LineTotal);
//      }
//      // Read the subtotal, shipping cost, and total cost.
//      Console.WriteLine("\t\t\t\t\t Subtotal\t" + po.SubTotal);
//      Console.WriteLine("\t\t\t\t\t Shipping\t" + po.ShipCost);
//      Console.WriteLine("\t\t\t\t\t Total\t\t" + po.TotalCost);
//   }
//
//   protected void ReadAddress(Address a, string label)
//   {
//      // Read the fields of the Address object.
//      Console.WriteLine(label);
//      Console.WriteLine("\t"+ a.Name );
//      Console.WriteLine("\t" + a.Line1);
//      Console.WriteLine("\t" + a.City);
//      Console.WriteLine("\t" + a.State);
//      Console.WriteLine("\t" + a.Zip );
//      Console.WriteLine();
//   }
//
//   private void serializer_UnknownNode
//   (object sender, XmlNodeEventArgs e)
//   {
//      Console.WriteLine("Unknown Node:" +   e.Name + "\t" + e.Text);
//   }
//
//   private void serializer_UnknownAttribute
//   (object sender, XmlAttributeEventArgs e)
//   {
//      System.Xml.XmlAttribute attr = e.Attr;
//      Console.WriteLine("Unknown attribute " +
//      attr.Name + "='" + attr.Value + "'");
//   }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace XamlFormatter
{
    public class XamlXmlFormatter
    {
        // Bug KeyBindings Element - should not change Key to x:Key
        private readonly List<string> keyedElements = new List<string>();
        private readonly List<string> namedElements = new List<string>();
        private readonly List<string> namespaces = new List<string>();
        private readonly Dictionary<string, string> replaceList = new Dictionary<string, string>();
        private readonly List<string> unusedKeys = new List<string>();
        private readonly List<string> unusedNames = new List<string>();
        private readonly List<string> unusedNamespaces = new List<string>();
        private int elementCount;

        /// <summary>
        ///     Initializes a new instance of the <see cref="XamlXmlFormatter" /> class.
        /// </summary>
        public XamlXmlFormatter()
        {
            // Initialisation Stuff bere...
            IndentSize = 4;
            this.replaceList.Add("#00FFFFFF", "Transparent");
            this.replaceList.Add("#FF000000", "Black");
            this.replaceList.Add("#FFFFFFFF", "White");
        }

        /// <summary>
        ///     Gets or sets the size of the indent.
        /// </summary>
        /// <value>The size of the indent.</value>
        public int IndentSize { get; set; }

        public ICollection<string> KeyedElements
        {
            get { return this.keyedElements; }
        }

        public ICollection<string> NamedElements
        {
            get { return this.namedElements; }
        }

        public ICollection<string> UnusedKeys
        {
            get { return this.unusedKeys; }
        }

        public ICollection<string> UnusedNames
        {
            get { return this.unusedNames; }
        }

        public ICollection<string> UnusedNamespaces
        {
            get { return this.unusedNamespaces; }
        }

        public void Format(string fileName)
        {
            Format(fileName, fileName);
        }

        public void Format(string sourcefileName, string destinationFileName)
        {
            XDocument doc = GetDocument(sourcefileName);
            string formatted = FormatDocument(doc);

            File.WriteAllText(destinationFileName, formatted);

            try
            {
                // Re-read the document in to ensure output is well-formed
                GetDocument(destinationFileName);
            }
            catch (XmlException ex)
            {
                throw new XmlException("A bug has been detected in the formatter - the transformed output is not well-formed xml", ex);
            }
        }

        public string FormatText(string xamlText)
        {
            XDocument doc = XDocument.Parse(xamlText);
            return FormatDocument(doc);
        }

        private static string Encode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static XDocument GetDocument(string fileName)
        {
            string contents = File.ReadAllText(fileName);
            return XDocument.Parse(contents);
        }

        private static bool IsOneLineElement(XElement element)
        {
            var oneLineElementNames = new[] { "Setter", "Trigger", "DataTrigger", "Condition" };
            return oneLineElementNames.Contains(element.Name.LocalName);
        }

        private static bool IsSpaceAfterElement(XElement element)
        {
            if (element.Name.LocalName.EndsWith(".MergedDictionaries"))
            {
                return true;
            }

            if (element.Ancestors().Count() == 1)
            {
                return true;
            }

            return false;
        }

        private static string Space(int spaces)
        {
            return string.Join(string.Empty, Enumerable.Repeat(" ", spaces));
        }

        private void CheckForUsages(string xaml)
        {
            // Check usages of names and keys
            string search = @"(\""|\s|=){0}(\""|\}}|\s|,)";
            foreach (string name in this.namedElements)
            {
                if (Regex.Matches(xaml, string.Format(search, name)).Count < 2)
                {
                    // Not used in xaml
                    this.unusedNames.Add(name);
                }
            }

            foreach (string key in this.keyedElements)
            {
                if (Regex.Matches(xaml, string.Format(search, key)).Count < 2)
                {
                    // Not used in xaml
                    this.unusedKeys.Add(key);
                }
            }

            search = @"(<|\s|""){0}:";
            foreach (string ns in this.namespaces)
            {
                if (!Regex.IsMatch(xaml, string.Format(search, ns)))
                {
                    this.unusedNamespaces.Add(ns);
                }
            }
        }

        private string FormatDocument(XDocument doc)
        {
            try
            {
                Initialise();
                int indent = 0;
                var builder = new StringBuilder();
                var writer = new StringWriter(builder);

                foreach (XElement element in doc.Elements())
                {
                    RecurseElements(element, indent, writer);
                }

                string formatted = builder.ToString();
                CheckForUsages(formatted);
                return formatted;
            }
            catch (XmlException ex)
            {
                throw new ApplicationException("Badly formatted Xml", ex);
            }
        }

        private void Initialise()
        {
            this.namedElements.Clear();
            this.unusedNames.Clear();
            this.keyedElements.Clear();
            this.unusedKeys.Clear();
            this.namespaces.Clear();
            this.unusedNamespaces.Clear();
            this.elementCount = 0;
        }

        private bool IsSpaceBeforeElement(XElement element)
        {
            if (this.elementCount == 2)
            {
                return true;
            }
            switch (element.Name.LocalName)
            {
                case "Style":
                case "DataTemplate":
                case "ControlTemplate":
                    return true;

                default:
                    return false;
            }
        }

        private void RecurseElements(XElement element, int indent, TextWriter writer)
        {
            this.elementCount++;
            string thisIndent = Space(indent);
            string nextIndent = Space(indent + IndentSize);
            string ns = element.GetPrefixOfNamespace(element.Name.Namespace);
            bool hasChildren = element.HasElements;
            if (!String.IsNullOrEmpty(element.Value))
            {
                hasChildren = true;
            }

            if (ns == null)
            {
                ns = string.Empty;
            }
            else
            {
                ns = ns + ":";
            }

            // Opening glyph
            if (IsSpaceBeforeElement(element))
            {
                writer.WriteLine(string.Empty);
            }

            writer.Write("{0}<{1}{2}", thisIndent, ns, element.Name.LocalName);

            WriteAttributes(writer, element, nextIndent);

            if (hasChildren)
            {
                WriteChildren(writer, element, indent, nextIndent);
            }

            // Write Closing glyphs
            if (hasChildren)
            {
                writer.WriteLine("{0}</{1}{2}>", thisIndent, ns, element.Name.LocalName);
            }
            else
            {
                writer.WriteLine(" />");
            }
            if (IsSpaceAfterElement(element))
            {
                writer.WriteLine(string.Empty);
            }
        }

        private string SpecialAttribHandling(XAttribute attrib)
        {
            switch (attrib.Name.LocalName)
            {
                case "Name":
                    this.namedElements.Add(attrib.Value);
                    if (String.IsNullOrEmpty(attrib.Name.Namespace.NamespaceName))
                    {
                        return String.Format("x:{0}", attrib);
                    }

                    return attrib.ToString();

                case "Key":
                    this.keyedElements.Add(attrib.Value);
                    if (String.IsNullOrEmpty(attrib.Name.Namespace.NamespaceName))
                    {
                        return String.Format("x:{0}", attrib);
                    }

                    return attrib.ToString();

                default:
                    if (attrib.Value == "#FF000000")
                    {
                        Debugger.Break();
                    }

                    if (attrib.ToString().StartsWith("xmlns:"))
                    {
                        this.namespaces.Add(attrib.Name.LocalName);
                    }
                    if (this.replaceList.ContainsKey(attrib.Value))
                    {
                        return String.Format("{1}=\"{0}\"", this.replaceList[attrib.Value], attrib.Name.LocalName);
                    }

                    return attrib.ToString();
            }
        }

        private void WriteAttributes(TextWriter writer, XElement element, string nextIndent)
        {
            if (element.HasAttributes)
            {
                var attribs = new List<string>(element.Attributes().Count());

                // Enumerate attribs and place into array for sorting
                foreach (XAttribute attrib in element.Attributes())
                {
                    attribs.Add(SpecialAttribHandling(attrib));
                }
                try
                {
                    attribs.Sort(new XamlAttributeComparer());
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(String.Format("Exception thrown while sorting [{0}]", String.Join(", ", attribs.ToArray())), ex);
                }

                bool putOnOneLine = false;
                putOnOneLine = (attribs.Count == 1 && attribs[0].Length + nextIndent.Length <= 200);
                if (!putOnOneLine && IsOneLineElement(element))
                {
                    putOnOneLine = true;
                }

                foreach (string attrib in attribs)
                {
                    if (putOnOneLine)
                    {
                        writer.Write(" ");
                    }
                    else
                    {
                        writer.WriteLine(" ");
                        writer.Write(nextIndent);
                    }
                    writer.Write(attrib);
                }
            }
        }

        private void WriteChildren(TextWriter writer, XElement element, int indent, string nextIndent)
        {
            writer.WriteLine(">");
            foreach (XNode child in element.Nodes())
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    RecurseElements(child as XElement, indent + IndentSize, writer);
                }
                if (child.NodeType == XmlNodeType.Text && child is XText)
                {
                    writer.WriteLine("{0}{1}", nextIndent, Encode((child as XText).Value.Trim()));
                }
                if (child.NodeType == XmlNodeType.Comment)
                {
                    writer.WriteLine("{0}{1}", nextIndent, child);
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;

namespace XamlFormatter {
    public class XamlAttributeComparer : IComparer<string> {

        public int Compare(string x, string y) {
            if (String.IsNullOrEmpty(x) && !String.IsNullOrEmpty(y)) return -1;
            if (!String.IsNullOrEmpty(x) && String.IsNullOrEmpty(y)) return 1;
            if (String.IsNullOrEmpty(x) && String.IsNullOrEmpty(y)) return 0;
            if (String.Compare(x, y, true) == 0) return 0;

            char separator = '=';
            string[] x1 = x.Split(separator);
            string[] y1 = y.Split(separator);

            //x:Key, x:Class, x:Name all take top priority and should come first
            if (x1[0].StartsWith("x:") && !y1[0].StartsWith("x:")) {
                return -1;
            }
            if (y1[0].StartsWith("x:") && !x1[0].StartsWith("x:")) {
                return 1;
            }

            //xmlns should come next
            if (String.Compare(x1[0], "xmlns", true) == 0) {
                return -1;
            }
            if (String.Compare(y1[0], "xmlns", true) == 0) {
                return 1;
            }

            //Any ns spec should come next
            if (x1[0].Contains(":") && !y1[0].Contains(":")) {
                return -1;
            }
            if (y1[0].Contains(":") && !x1[0].Contains(":")) {
                return 1;
            }

            return String.Compare(x, y, true);
        }

    }
}

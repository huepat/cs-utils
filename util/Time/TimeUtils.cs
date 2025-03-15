using HuePat.Util.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuePat.Util.Time {
    public static class TimeUtils {
        public class Timestamped<T> {
            public double Timestamp { get; private set; }
            public T Element { get; private set; }

            public Timestamped(
                    double timestamp, 
                    T element) {

                Timestamp = timestamp;
                Element = element;
            }
        }

        public class Associated<T, U> {
            public Timestamped<T> Element1 { get; private set; }
            public Timestamped<U> Element2 { get; private set; }

            public Associated(
                    Timestamped<T> element1,
                    Timestamped<U> element2) {

                Element1 = element1;
                Element2 = element2;
            }
        }

        public static string GetTimestampString() {

            return DateTime
                .Now
                .ToString("yyyyMMdd_HHmmss");
        }

        public static List<Timestamped<Associated<T, U>>> ToTimestamped<T, U>(
                this List<Associated<T, U>> associatedElements) {

            return associatedElements
                .Select(element => new Timestamped<Associated<T, U>>(
                    element.Element1.Timestamp,
                    element))
                .ToList();
        }

        public static void Print<T, U>(
                this List<Associated<T, U>> associatedElements) {

            foreach (Associated<T, U> associatedElement in associatedElements) {

                System.Console.WriteLine(
                    $"{associatedElement.Element1.Element} => {associatedElement.Element2.Element} "
                        + $"[{associatedElement.Element1.Timestamp - associatedElement.Element2.Timestamp}]");
            }
        }

        public static List<Associated<T, U>> Associate<T, U>(
                List<Timestamped<T>> elements1,
                List<Timestamped<U>> elements2,
                double maxDifference) {

            Associated<T, U>[] associatedElements = new Associated<T, U>[elements1.Count];

            Parallel.For(
                0,
                elements1.Count,
                i => {

                    List<Timestamped<U>> candidates = elements2.WhereMin(
                        element2 => (element2.Timestamp - elements1[i].Timestamp).Abs()
                    );

                    Timestamped<U> associatedElement = null;

                    if (candidates.Count > 0 
                            && (candidates.First().Timestamp - elements1[i].Timestamp).Abs() <= maxDifference) {

                        associatedElement = candidates.First();
                    }

                    associatedElements[i] = new Associated<T, U>(
                        elements1[i],
                        associatedElement);
                });

            return associatedElements.ToList();
        }
    }
}
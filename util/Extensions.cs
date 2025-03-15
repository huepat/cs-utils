using HuePat.Util.Math;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace HuePat.Util {
    public static class Extensions {
        public static string Format(
                this double value,
                int? numberOfDigits = null) {

            if (numberOfDigits.HasValue) {

                return string.Format(
                    "{0:0." + new string('0', numberOfDigits.Value) + "}",
                    value);
            }
            
            return value.ToString();
        }

        public static int GetNumberOfDigits(
                this int value) {

            return (int)(((double)value).Log10() + 1.0).Floor();
        }

        public static T[] Copy<T>(
                this T[] array) {

            T[] copy = new T[array.Length];

            for (int i = 0; i < array.Length; i++) {
                copy[i] = array[i];
            }

            return copy;
        }

        public static string Remove(
                this string s,
                string substring) {

            return s.Replace(
                substring,
                "");
        }

        public static string[] Split(
                this string s,
                string separator,
                StringSplitOptions stringSplitOptions = StringSplitOptions.None) {

            return s.Split(
                new string[] { separator },
                stringSplitOptions);
        }

        public static IEnumerable<T> Unpack<T>(
                this IEnumerable<IEnumerable<T>> values) {

            return values.SelectMany(valueSet => valueSet);
        }

        public static bool All(
                this IEnumerable<bool> values) {

            return values.All(value => value);
        }

        public static bool Any(
                this IEnumerable<bool> values) {

            return values.Any(value => value);
        }

        public static bool Any(
                this bool[,] array) {

            return Enumerable
                .Range(0, array.GetLength(0))
                .Any(x => Enumerable
                    .Range(0, array.GetLength(1))
                    .Any(y => array[x, y]));
        }

        public static T FirstOr<T>(
                this IEnumerable<T> values,
                T orValue) {

            if (values.Any()) {
                return values.First();
            }

            return orValue;
        }

        public static T[,] ToArray2D<T>(
                this T[][] array,
                int sizeSecondDimension) {

            int length;

            T[,] result = new T[
                array.Length,
                sizeSecondDimension];

            for (int i = 0; i < array.Length; i++) {

                length = array[i].Length;

                if (length != sizeSecondDimension) {
                    throw new ArgumentException();
                }

                for (int j = 0; j < length; j++) {
                    result[i, j] = array[i][j];
                }
            }

            return result;
        }

        public static T[][] ToArrayOfArrays<T>(
                this T[,] array) {

            T[][] result = new T[array.GetLength(0)][];

            for (int i = 0; i < array.GetLength(0); i++) {

                result[i] = new T[array.GetLength(1)];

                for (int j = 0; j < array.GetLength(1); j++) {
                    result[i][j] = array[i, j];
                }
            }

            return result;
        }

        public static (T, T) MinTuple<T>(
                this IEnumerable<(T, T)> tuples) 
                    where T : IComparable {

            bool first = true;
            (T, T) min = (default(T), default(T));

            foreach ((T, T) tuple in tuples) {

                if (first) {

                    first = false;
                    min = tuple;
                }
                else  {

                    if (tuple.Item1.CompareTo(min.Item1) < 0) {
                        min.Item1 = tuple.Item1;
                    }
                    if (tuple.Item2.CompareTo(min.Item2) < 0) {
                        min.Item2 = tuple.Item2;
                    }
                }
            }

            return min;
        }

        public static (T, T) MinMax<T>(
                this IEnumerable<T> values) 
                    where T : IComparable {

            bool first = true;

            T min = default(T);
            T max = default(T);

            foreach (T value in values) {

                if (first) {

                    first = false;
                    min = value;    
                    max = value;
                }
                else {

                    if (value.CompareTo(min) < 0) {
                        min = value;
                    }
                    if (value.CompareTo(max) > 0) {
                        max = value;
                    }
                }
            }

            return (min, max);
        }

        public static ((T, T), (T, T)) MinMax<T>(
                this IEnumerable<(T, T)> values)
                    where T : IComparable {

            bool first = true;

            (T, T) min = (default(T), default(T));
            (T, T) max = (default(T), default(T));

            foreach ((T, T) value in values) {

                if (first) {

                    first = false;
                    min = value;
                    max = value;
                }
                else {

                    if (value.Item1.CompareTo(min.Item1) < 0) {
                        min.Item1 = value.Item1;
                    }
                    if (value.Item2.CompareTo(min.Item2) < 0) {
                        min.Item2 = value.Item2;
                    }
                    if (value.Item1.CompareTo(max.Item1) > 0) {
                        max.Item1 = value.Item1;
                    }
                    if (value.Item2.CompareTo(max.Item2) > 0) {
                        max.Item2 = value.Item2;
                    }
                }
            }

            return (min, max);
        }

        public static void AddRange<T>(
                this HashSet<T> set,
                IEnumerable<T> values) {

            foreach (T value in values) {
                set.Add(value);
            }
        }

        public static void Remove<T>(
                this HashSet<T> set,
                IEnumerable<T> values) {

            foreach (T value in values) {
                set.Remove(value);
            }
        }

        public static void RemoveAt<T>(
                this List<T> list,
                IEnumerable<int> indices) {

            foreach (int i in indices.OrderDescending()) {
                list.RemoveAt(i);
            }
        }

        public static void Remove<TKey, TValue>(
                this IDictionary<TKey, TValue> dictionary,
                IEnumerable<TKey> keys) {

            foreach (TKey key in keys) {
                dictionary.Remove(key);
            }
        }

        public static List<T> Shuffled<T>(
                this IList<T> list) {

            List<T> shuffled = new List<T>(list);

            shuffled.Shuffle();

            return shuffled;
        }

        public static List<T> Shuffled<T>(
                this IReadOnlyList<T> list) {

            List<T> shuffled = new List<T>(list);

            shuffled.Shuffle();

            return shuffled;
        }

        public static void Shuffle<T>(
                this IList<T> list) {

            int k, n;

            n = list.Count;

            while (n > 1) {
                n--;
                k = Random.GetInteger(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void ForEach<T>(
                this IEnumerable<T> values,
                Action<T> callback) {

            foreach (T value in values) {
                callback(value);
            }
        }

        public static IEnumerable<T> Order<T>(
                this IEnumerable<T> values) {

            return values.OrderBy(value => value);
        }

        public static IEnumerable<T> OrderDescending<T>(
                this IEnumerable<T> values) {

            return values.OrderByDescending(value => value);
        }

        public static T[] Concat<T>(
                this T value, 
                T[] array) {

            T[] newArray = new T[array.Length + 1];

            newArray[0] = value;
            array.CopyTo(newArray, 1);

            return newArray;
        }

        public static IEnumerable<T> Concat<T>(
                this T value, 
                IEnumerable<T> values) {

            return new T[] { value }.Concat(values);
        }

        public static void Dispose(
                this IEnumerable<IDisposable> disposables) {

            foreach (IDisposable disposable in disposables) {
                disposable.Dispose();
            }
        }

        public static void Dispose<T, U>(
                this IDictionary<T, U> disposables) where U : IDisposable {

            foreach (IDisposable disposable in disposables.Values) {
                disposable.Dispose();
            }
        }

        public static IEnumerable<string> ToStrings<T>(
                this IEnumerable<T> values) {
            return values
                .Select(value => value.ToString());
        }

        public static string Join(
                this IEnumerable<string> strings,
                string seperator = ", ") {

            return string.Join(seperator, strings);
        }

        public static byte[] Serialize(
                this object @object) {

            using (MemoryStream memoryStream = new MemoryStream()) {

                new BinaryFormatter().Serialize(
                    memoryStream, 
                    @object);

                return memoryStream.ToArray();
            }
        }

        public static IEnumerable<T> Concat<T>(
                this IEnumerable<T> values, 
                T value) {

            return values.Concat(new T[] { value });
        }

        public static List<T> WhereMin<T>(
                this IEnumerable<T> objects) 
                    where T : IComparable {

            return WhereMin(objects, @object => @object);
        }

        public static List<T> WhereMin<T, U>(
                this IEnumerable<T> objects,
                Func<T, U> valueExtractor) 
                    where U : IComparable {

            if (!objects.Any()) {
                return new List<T>();
            }

            bool first = true;
            int testResult;
            List<T> resultObjects = new List<T> { objects.First() };
            U candidateValue = valueExtractor(resultObjects[0]);
            U testValue;

            foreach (T @object in objects) {

                testValue = valueExtractor(@object);
                testResult = testValue.CompareTo(candidateValue);

                if (testResult < 0) {

                    candidateValue = testValue;
                    resultObjects = new List<T> { 
                        @object 
                    };
                }
                else if (!first 
                        && testResult == 0) {

                    resultObjects.Add(@object);
                }

                first = false;
            }

            return resultObjects;
        }

        public static List<T> WhereMax<T>(
                this IEnumerable<T> objects) 
                    where T : IComparable {

            return WhereMax(objects, @object => @object);
        }

        public static List<T> WhereMax<T, U>(
                this IEnumerable<T> objects,
                Func<T, U> valueExtractor) 
                    where U : IComparable {

            if (!objects.Any()) {
                return new List<T>();
            }

            bool first = true;
            int testResult;
            List<T> resultObjects = new List<T> { objects.First() }; 
            U candidateValue = valueExtractor(resultObjects[0]);
            U testValue;

            foreach (T @object in objects) {

                testValue = valueExtractor(@object);
                testResult = testValue.CompareTo(candidateValue);

                if (testResult > 0) {

                    candidateValue = testValue;
                    resultObjects = new List<T> { 
                        @object 
                    };
                }
                else if(!first 
                        && testResult == 0) {

                    resultObjects.Add(@object);
                }

                first = false;
            }

            return resultObjects;
        }

        public static IEnumerable<double> ApproximateDistinct(
                this IEnumerable<double> values,
                double epsilon = double.Epsilon) {

            List<double> distinctValues = new List<double>();

            foreach (double value in values) {

                if (!distinctValues.ApproximateContains(value, epsilon)) {
                    distinctValues.Add(value);
                }
            }

            return distinctValues;
        }

        public static bool ApproximateContains(
                this IEnumerable<double> values, 
                double value, 
                double epsilon = double.Epsilon) {

            foreach (double v in values) {
                if (v.ApproximateEquals(value, epsilon)) {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<T> TakeEveryX<T>(
                this IEnumerable<T> values,
                int x) {

            int i = 0;

            foreach (T value in values) {

                if (i++ % x == 0) {

                    yield return value;
                }
            }
        }

        public static T[] Concat<T>(
                this T[] array, 
                T value) {

            T[] newArray = new T[array.Length + 1];

            array.CopyTo(newArray, 0);
            newArray[array.Length] = value;

            return newArray;
        }

        public static T[] Concat<T>(
                this T[] array1, 
                T[] array2) {

            T[] newArray = new T[array1.Length + array2.Length];

            array1.CopyTo(newArray, 0);
            array2.CopyTo(newArray, array1.Length);

            return newArray;
        }

        public static T[] Concat<T>(
                this T[] array, 
                IEnumerable<T> values) {

            int i = array.Length;
            T[] newArray = new T[i + values.Count()];

            array.CopyTo(newArray, 0);

            foreach (T value in values) {
                newArray[i++] = value;
            }

            return newArray;
        }

        public static T[] Populate<T>(
                this T[] array, 
                T value) {

            for (int i = 0; i < array.Length; i++) {
                array[i] = value;
            }

            return array;
        }

        public static bool AreDistinct(
                this IList<int> list) {

            for (int i = 0; i < list.Count; i++) {
                for (int j = i + 1; j < list.Count; j++) {

                    if (i != j 
                            && list[i] == list[j]) {

                        return false;
                    }
                }
            }

            return true;
        }

        public static bool AreDistinct<T>(
                this IEnumerable<T> values) {

            return values.Distinct().Count() == values.Count();
        }

        public static T GetMaxValueKey<T>(
                this Dictionary<T, int> dictionary) {

            T maxValueKey = dictionary.Keys.First();

            foreach (T key in dictionary.Keys) {
                if (dictionary[key] > dictionary[maxValueKey]) {
                    maxValueKey = key;
                }
            }

            return maxValueKey;
        }

        public static void AddOrOverwrite<TKey, TValue>(
                this Dictionary<TKey, TValue> dictionary,
                TKey key,
                TValue value) {

            if (dictionary.ContainsKey(key)) {
                dictionary[key] = value;
            }
            else {
                dictionary.Add(key, value);
            }
        }

        public static void AddOrOverwrite<TKey, TValue>(
                this Dictionary<TKey, TValue> dictionary,
                Dictionary<TKey, TValue> otherDictionary) {

            foreach (TKey key in otherDictionary.Keys) {

                dictionary.AddOrOverwrite(
                    key,
                    otherDictionary[key]);
            }
        }

        public static void AddOrOverwrite<TKey, TValue>(
                this Dictionary<TKey, TValue> dictionary,
                IEnumerable<TKey> keys,
                TValue value) {

            foreach (TKey key in keys) {
                dictionary.AddOrOverwrite(key, value);
            }
        }

        public static void BucketAdd<TKey, TValue>(
                this Dictionary<TKey, List<TValue>> dictionary,
                TKey key,
                TValue value) {

            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, new List<TValue>());
            }

            dictionary[key].Add(value);
        }

        public static void BucketAdd<TKey, TValue>(
                this Dictionary<TKey, List<TValue>> dictionary,
                TKey key,
                IEnumerable<TValue> values) {

            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, new List<TValue>());
            }

            dictionary[key].AddRange(values);
        }

        public static void BucketAdd<TKey, TValue>(
                this Dictionary<TKey, List<TValue>> dictionary1,
                Dictionary<TKey, List<TValue>> dictionary2) {

            foreach (TKey key in dictionary2.Keys) {

                if (!dictionary1.ContainsKey(key)) {
                    dictionary1.Add(key, new List<TValue>());
                }

                dictionary1[key].AddRange(dictionary2[key]);
            }
        }

        public static void BucketAdd<TKey, TValue>(
                this Dictionary<TKey, HashSet<TValue>> dictionary,
                TKey key,
                TValue value) {

            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, new HashSet<TValue>());
            }

            dictionary[key].Add(value);
        }

        public static void BucketAdd<TKey, TValue>(
                this Dictionary<TKey, HashSet<TValue>> dictionary,
                TKey key,
                IEnumerable<TValue> values) {

            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, new HashSet<TValue>());
            }

            dictionary[key].AddRange(values);
        }

        public static void BucketAdd<TKey, TValue>(
                this Dictionary<TKey, HashSet<TValue>> dictionary1,
                Dictionary<TKey, HashSet<TValue>> dictionary2) {

            foreach (TKey key in dictionary2.Keys) {

                if (!dictionary1.ContainsKey(key)) {
                    dictionary1.Add(key, new HashSet<TValue>());
                }

                dictionary1[key].AddRange(dictionary2[key]);
            }
        }

        public static void BucketAdd<T>(
                this Dictionary<T, int> dictionary,
                T key,
                int value) {

            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, 0);
            }

            dictionary[key] += value;
        }

        public static void BucketIncrement<T>(
                this Dictionary<T, int> dictionary, 
                T key) {

            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, 0);
            }

            dictionary[key]++;
        }

        public static void BucketAdd<T>(
                this Dictionary<T, int> dictionary,
                Dictionary<T, int> dictionary2) {

            foreach (T key in dictionary2.Keys) {

                if (!dictionary.ContainsKey(key)) {
                    dictionary.Add(key, 0);
                }

                dictionary[key] += dictionary2[key];
            }
        }

        public static void BucketAdd<T>(
                this Dictionary<T, double> dictionary,
                Dictionary<T, double> dictionary2) {

            foreach (T key in dictionary2.Keys) {

                if (!dictionary.ContainsKey(key)) {
                    dictionary.Add(key, 0.0);
                }

                dictionary[key] += dictionary2[key];
            }
        }

        public static List<T> GetMaxKeys<T>(
                this Dictionary<T, int> dictionary) {

            return dictionary
                .Keys
                .WhereMax(key => dictionary[key]);
        }

        public static int GetMaxCount<T>(
                this Dictionary<T, int> dictionary) {

            int count;
            int maxCount = int.MinValue;

            foreach (T key in dictionary.Keys) {

                count = dictionary[key];

                if (count > maxCount) {
                    maxCount = count;
                }
            }

            return maxCount;
        }

        public static void AddIfExists<T>(
                this List<T> list, 
                T element) 
                    where T : class {

            if(element != null) {
                list.Add(element);
            }
        }
    }
}
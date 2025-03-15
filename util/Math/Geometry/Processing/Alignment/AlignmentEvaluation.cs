using HuePat.Util.IO;
using HuePat.Util.IO.PLY.Reading;
using HuePat.Util.IO.PLY.Writing;
using HuePat.Util.Math.Geometry.Processing.Rotating;
using HuePat.Util.Math.Statistics;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace HuePat.Util.Math.Geometry.Processing.Alignment {
    public static class AlignmentEvaluation {
        public enum Type {
            MESH,
            POINT_CLOUD
        };

        public class Config {
            private int sampleCount;
            private IList<Matrix3d> rotations;

            public bool OutputPly { get; set; }
            public bool NormalizeAlignment { get; set; }
            public Type Type { get; private set; }
            public int LineBreakByteSize { get; set; }
            public double VerticalAngleRange { private get; set; }
            public string File { get; private set; }
            public Vector3d UpAxis { get; private set; }
            public Vector3d HorizontalAxis { get; private set; }
            public Vector3d HorizontalAxis2 { get; private set; }

            public int SampleCount { 
                get {
                    return sampleCount;
                }
                set {
                    sampleCount = value;
                    rotations = null;
                }
            }

            public IList<Matrix3d> Rotations {
                get {

                    if (rotations != null) {
                        return rotations;
                    }

                    Angles = Enumerable
                        .Range(0, SampleCount)
                        .Select(i => (
                            Random.GetDouble(0.0, 360.0.DegreeToRadian()),
                            Random.GetDouble(-VerticalAngleRange, VerticalAngleRange),
                            Random.GetDouble(-VerticalAngleRange, VerticalAngleRange)))
                        .ToList();

                    return rotations;
                }
                set {
                    sampleCount = value.Count;
                    rotations = value;
                }
            }

            public IList<(double, double, double)> Angles {
                set {

                    Trace.WriteLine("");
                    Trace.WriteLine("INPUT ANGLES:");

                    for (int i = 0; i < value.Count(); i++) {

                        Trace.WriteLine($"    {i}: (" +
                            $"{value[i].Item1.RadianToDegree():0.00}°, " +
                            $"{value[i].Item2.RadianToDegree():0.00}°, " +
                            $"{value[i].Item3.RadianToDegree():0.00}°)");
                    }

                    Trace.WriteLine("");

                    sampleCount = value.Count;

                    rotations = value
                        .Select(angleSet => 
                            HorizontalAxis2.GetRotationAround(angleSet.Item3)
                                * HorizontalAxis.GetRotationAround(angleSet.Item2)
                                * UpAxis.GetRotationAround(angleSet.Item1))
                        .ToList();
                }
            }

            public Config(
                    double verticalAngleRange,
                    string file,
                    Type type,
                    Vector3d upAxis,
                    Vector3d horizontalAxis) {

                VerticalAngleRange = verticalAngleRange;
                SampleCount = 10;
                LineBreakByteSize = 1;
                File = file;
                Type = type;

                UpAxis = upAxis;
                HorizontalAxis = horizontalAxis;
                HorizontalAxis2 = Vector3d.Cross(UpAxis, HorizontalAxis);

                Trace.WriteLine("");
                Trace.WriteLine($"UP AXIS: ({UpAxis.X}, {UpAxis.Y}, {UpAxis.Z})");
                Trace.WriteLine($"HORIZONTAL AXIS 1: ({HorizontalAxis.X}, {HorizontalAxis.Y}, {HorizontalAxis.Z})");
                Trace.WriteLine($"HORIZONTAL AXIS 2: ({HorizontalAxis2.X}, {HorizontalAxis2.Y}, {HorizontalAxis2.Z})");
                Trace.WriteLine("");
            }

            public Config LoadAnglesFromEvaluationLog(
                    string file) {

                bool isAnglesSection = false;
                string[] values;
                List<(double, double, double)> angles = new List<(double, double, double)>();

                foreach (string line in System.IO.File.ReadAllLines(file)) {

                    if (isAnglesSection) {

                        if (!line.Contains(":")) {
                            break;
                        }

                        values = line
                            .Split("(")[1]
                            .Split("°)")[0]
                            .Split("°,");

                        angles.Add((
                            double.Parse(values[0]).DegreeToRadian(),
                            double.Parse(values[1]).DegreeToRadian(),
                            double.Parse(values[2]).DegreeToRadian()));
                    }
                    else if(line.StartsWith("INPUT ANGLES:")) {
                        isAnglesSection = true;
                    }
                }

                Angles = angles;

                return this;
            }
        }

        public const string DIRECTORY = @"C:\Users\phuebner\data\test\alignment";

        public static void Evaluate(
                Config config) {

            if (!Directory.Exists(DIRECTORY)) {
                Directory.CreateDirectory(DIRECTORY);
            }

            FileSystemUtils.CleanDirectory(DIRECTORY);

            int i = 0;
            (double, double) referenceAngles;
            (double, double) angleDeviations;
            IShape clone;
            IShape model = Load(config);
            Matrix3d rotation;
            Point point;
            PlyStreamWriter writer = null;
            (AngleStatistics, AngleStatistics) angleDeviationStatistics = (
                new AngleStatistics(),
                new AngleStatistics());

            IList<Matrix3d> rotations = config.Rotations;

            Trace.WriteLine("EVALUATION:");

            if (config.OutputPly) {

                model.Export(
                    $"{DIRECTORY}/GroundTruth.ply",
                    config);

                writer = new PlyStreamWriter($"{DIRECTORY}/Results.ply") { 
                    PointFormat = new PointFormat() { 
                        PropertyDescriptor = new PropertyDescriptor()
                            .AddFloatProperty("d_h")
                            .AddFloatProperty("d_v")
                    }
                };
            }

            foreach (Matrix3d referenceRotation in rotations) {
                clone = model.Clone();
                clone.Rotate(
                    new Rotation(referenceRotation) {
                        UseParallel = true,
                        RotateNormals = config.Type == Type.POINT_CLOUD
                    });
                if (config.OutputPly) {
                    clone.Export(
                        $"{DIRECTORY}/In_{i}.ply",
                        config);
                }
                clone.Align(
                    config.NormalizeAlignment,
                    config.UpAxis,
                    config.HorizontalAxis,
                    out rotation);
                referenceAngles = (
                    config.HorizontalAxis
                        .AngleTo(
                            referenceRotation.Multiply(config.HorizontalAxis))
                        .Abs(),
                    config.UpAxis
                        .AngleTo(
                            referenceRotation.Multiply(config.UpAxis))
                        .Abs());
                angleDeviations = (
                    config.HorizontalAxis
                        .AngleTo(
                            rotation.Multiply(
                                referenceRotation.Multiply(config.HorizontalAxis)))
                        .Abs(),
                    config.UpAxis
                        .AngleTo(
                            rotation.Multiply(
                                referenceRotation.Multiply(config.UpAxis)))
                        .Abs());
                while (angleDeviations.Item1 >= 45.0.DegreeToRadian()) {
                    angleDeviations.Item1 -= 90.0.DegreeToRadian();
                }
                angleDeviations.Item1 = angleDeviations.Item1.Abs();
                if (config.OutputPly) {
                    point = new Point(
                        referenceRotation.Multiply(config.UpAxis));
                    point.SetFloatProperty("d_h", (float)angleDeviations.Item1.RadianToDegree());
                    point.SetFloatProperty("d_v", (float)angleDeviations.Item2.RadianToDegree());
                    writer.Write(point);
                    clone.Export(
                        $"{DIRECTORY}/Out_{i}.ply",
                        config);
                }
                angleDeviationStatistics.Item1.Update(angleDeviations.Item1);
                angleDeviationStatistics.Item2.Update(angleDeviations.Item2);
                Trace.WriteLine($"{i++}: " +
                    $"({referenceAngles.Item1.RadianToDegree():0.00}°, " +
                        $"{referenceAngles.Item2.RadianToDegree():0.00}°) -> " +
                    $"({angleDeviations.Item1.RadianToDegree():0.00}°, " +
                        $"{angleDeviations.Item2.RadianToDegree():0.00}°)");
            }
            Trace.WriteLine("___________________________________________________________");
            Trace.WriteLine(
                $"({angleDeviationStatistics.Item1.Mean.RadianToDegree():0.00}° " +
                    $"(+-{angleDeviationStatistics.Item1.StandardDeviation.RadianToDegree():0.00}°), " +
                $"{angleDeviationStatistics.Item2.Mean.RadianToDegree():0.00}° " +
                    $"(+-{angleDeviationStatistics.Item2.StandardDeviation.RadianToDegree():0.00}°))");
            if (config.OutputPly) {
                writer.Dispose();
            }
        }

        private static IShape Load(Config config) {
            PlyReader reader = new PlyReader() {
                LineBreakByteSize = config.LineBreakByteSize
            };
            switch (config.Type) {
                case Type.MESH:
                    return reader.ReadMesh(config.File);
                case Type.POINT_CLOUD:
                    reader.PointFormat = new PointFormat() {
                        PropertyDescriptor = new PropertyDescriptor()
                            .AddNormalVector()
                    };
                    return reader.ReadPointCloud(config.File);
                default:
                    throw new ArgumentException();
            }
        }

        private static void Export(
                this IShape model,
                string file,
                Config config) {
            PlyWriter writer = new PlyWriter();
            if (config.Type == Type.POINT_CLOUD) {
                writer.PointFormat = new PointFormat() {
                    PropertyDescriptor = new PropertyDescriptor()
                        .AddNormalVector()
                };
            }
            writer.Write(
                file,
                model.Mesh);
        }
    }
}
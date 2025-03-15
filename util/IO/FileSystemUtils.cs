using System;
using System.IO;

namespace HuePat.Util.IO {
    public static class FileSystemUtils {
        private const string TEMP_FILE_POSTFIX = "_temp";

        public static bool IsFile(
                string path) {

            return !IsDirectory(path);
        }

        public static bool IsDirectory(
                string path) {

            return File
                .GetAttributes(path)
                .HasFlag(FileAttributes.Directory);
        }

        public static void CleanDirectory(
                string directory) {

            DirectoryInfo directoryInfo = new DirectoryInfo(directory);

            foreach (FileInfo file in directoryInfo.GetFiles()) {

                file.Delete();
            }

            foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories()) {

                subDirectory.Delete(true);
            }
        }

        public static void CopyDirectory(
                string sourceDirectory,
                string destinationDirectory) {

            if (!Directory.Exists(sourceDirectory)) {
                throw new DirectoryNotFoundException();
            }

            if (Directory.Exists(destinationDirectory)) {
                Directory.Delete(destinationDirectory, true);
            }

            Directory.CreateDirectory(destinationDirectory);

            foreach (string file in Directory.GetFiles(sourceDirectory)) {

                File.Copy(
                    file,
                    $"{destinationDirectory}/{Path.GetFileName(file)}");
            }

            foreach (string directory in Directory.GetDirectories(sourceDirectory)) {

                CopyDirectory(
                    directory, 
                    $"{destinationDirectory}/{Path.GetFileName(directory)}");
            }
        }

        public static void InsertLine(
                string file, 
                string line, 
                long lineIndex) {

            long i = 0;
            string tempFile = GetTempFile(file);
            
            using (StreamWriter writer = new StreamWriter(tempFile)) {

                foreach (string fileLine in File.ReadLines(file)) {

                    if (i++ == lineIndex) {
                        writer.WriteLine(line);
                    }

                    writer.WriteLine(fileLine);
                }
            }

            File.Delete(file);
            File.Move(tempFile, file);
        }

        public static void ReplaceLine(
                string file, 
                string line, 
                long lineIndex) {

            long i = 0;
            string tempFile = GetTempFile(file);
            
            using (StreamWriter writer = new StreamWriter(tempFile)) {

                foreach (string fileLine in File.ReadLines(file)) {

                    if (i++ == lineIndex) {
                        writer.WriteLine(line);
                    }
                    else {
                        writer.WriteLine(fileLine);
                    }
                }
            }

            File.Delete(file);
            File.Move(tempFile, file);
        }

        public static string GetWithPostfixAndNewExtension(
                string file,
                string postfix,
                string newExtension) {

            return GetWithNewExtension(
                GetFileWithPostfix(
                    file,
                    postfix),
                newExtension);
        }

        public static string GetWithNewExtension(
                string file, 
                string newExtension) {

            return
                $"{Path.GetDirectoryName(file)}/" +
                $"{Path.GetFileNameWithoutExtension(file)}.{newExtension}";
        }

        public static string GetFileWithPostfix(
                string file, 
                string postfix) {

            return 
                $"{Path.GetDirectoryName(file)}/" +
                $"{Path.GetFileNameWithoutExtension(file)}{postfix}{Path.GetExtension(file)}";
        }

        public static string GetFileWithPrefix(
                string file, 
                string prefix) {

            return
                $"{Path.GetDirectoryName(file)}/" +
                $"{prefix}{Path.GetFileNameWithoutExtension(file)}{Path.GetExtension(file)}";
        }

        public static string GetTempFile(
                string file) {

            string tempFile = GetFileWithPostfix(
                file, 
                TEMP_FILE_POSTFIX);

            while (File.Exists(tempFile)) {

                tempFile = GetFileWithPostfix(
                    tempFile, 
                    TEMP_FILE_POSTFIX);
            }

            return tempFile;
        }

        public static long CountLineDifferences(
                string file1,
                string file2) {

            long counter = 0;
            string[] lines1 = File.ReadAllLines(file1);
            string[] lines2 = File.ReadAllLines(file2);

            if (lines1.Length != lines2.Length) {
                throw new ArgumentException("Files need to have same length.");
            }

            for (int i = 0; i < lines1.Length; i++) {
                if (lines1[i] != lines2[i]) {
                    counter++;
                }
            }

            return counter;
        }
    }
}
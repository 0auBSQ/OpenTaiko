using System.Diagnostics;
using System.IO;
using System.Text;

namespace TJAPlayer3
{
    internal static class CDTXCompanionFileFinder
    {
        internal static string FindFileName(
            string directory,
            string mainFileName,
            string expectedCompanionFileName)
        {
            var expectedCompanionPath = Path.Combine(directory, expectedCompanionFileName);

            if (File.Exists(expectedCompanionPath))
            {
                return expectedCompanionFileName;
            }

            // If we could not find the file by its exact provided name, in
            // the vast majority of cases it has been mangled during zip
            // compression by a zip tool which is not properly aware of
            // multi-byte encodings, Unicode, etc. When decompressed, such
            // zipped files end up a file names which are simply the raw bytes
            // of the Shift-JIS encoded form. Some of these bytes will be
            // invalid as characters of file names and will have been further
            // mangled, usually to a single underscore character.

            // To begin finding the right file, we first need to get the raw
            // bytes that would comprise the file name if encoded into
            // Shift-JIS.
            var encodedCompanionFileNameBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(expectedCompanionFileName);

            // Here we have a helper method that will be used to try finding
            // the file by interpreting the byte representation encoded
            // just above, this time in terms of some other encoding which
            // might be in use in the user's file system.
            bool TryFindViaDecodedFileName(string prefix, Encoding encoding, out string foundCompanionFileName)
            {
                var decodedCompanionFileName = DecodeToLegalFileName(encodedCompanionFileNameBytes, encoding);

                try
                {
                    if (!File.Exists(Path.Combine(directory, decodedCompanionFileName)))
                    {
                        foundCompanionFileName = null;
                        return false;
                    }
                }
                catch
                {
                    Trace.TraceWarning(
                        $"{nameof(CDTXCompanionFileFinder)} could not find expected file '{expectedCompanionPath}' and could not check the existence of a file via {prefix} '{encoding.EncodingName}'. Possible illegal file path when combining directory '{directory}' with encoded file name '{decodedCompanionFileName}'.");

                    foundCompanionFileName = null;
                    return false;
                }

                Trace.TraceInformation(
                    $"{nameof(CDTXCompanionFileFinder)} could not find expected file '{expectedCompanionPath}' but found '{decodedCompanionFileName}' via {prefix} '{encoding.EncodingName}', Code Page {encoding.CodePage}, Windows Code Page {encoding.WindowsCodePage}.");

                foundCompanionFileName = decodedCompanionFileName;
                return true;
            }

            // Attempt to find the file as if the companion file's name was
            // mangled into codepage 437 (effectively the legacy DOS codepage,
            // and the one used by zip tools that are not unicode aware.)
            // This step finds >99% of files with mangled names.
            if (TryFindViaDecodedFileName(
                "Encoding.GetEncoding(437)",
                Encoding.GetEncoding(437),
                out var foundCompanionFileNameViaEncoding437))
            {
                return foundCompanionFileNameViaEncoding437;
            }

            // Attempt to find the file as if the companion file's name
            // was mangled into this computer's default encoding. This case
            // has not been observed during testing on US English computers,
            // but it is safe to perform and may assist other locales.
            if (TryFindViaDecodedFileName(
                "Encoding.Default",
                Encoding.Default,
                out var foundCompanionFileNameViaEncodingDefault))
            {
                return foundCompanionFileNameViaEncodingDefault;
            }

            // If the companion file still cannot be found, try to find a file
            // with the expected extension but having the same file name as the
            // main file with which it is associated (in most use cases: the .tja file.)
            if (TryFindViaMainFileName(
                directory,
                mainFileName,
                expectedCompanionPath,
                out var foundCompanionFileNameByMainFileName))
            {
                return foundCompanionFileNameByMainFileName;
            }

            // If the file still cannot be found, try to find a single file
            // with the expected supplementary file extension. (If more than
            // one file is found with the same extension, we can't reliably
            // choose the right one of them.)
            if (TryFindViaCompanionFileExtension(
                directory,
                expectedCompanionPath,
                out var foundCompanionFileNameByExtension))
            {
                return foundCompanionFileNameByExtension;
            }

            // If the file still cannot be found, produce a warning
            // and return the original file name unchanged.

            Trace.TraceWarning(
                $"{nameof(CDTXCompanionFileFinder)} could not find expected file '{expectedCompanionPath}' by any available means.");

            return expectedCompanionFileName;
        }

        private static string DecodeToLegalFileName(byte[] encodedBytes, Encoding encoding)
        {
            // Decode and then replace characters which are illegal in file
            // names in all locales, except for the backslash character which
            // will be handled immediately after this.
            var decodedBeforeDirectoryRemoval = encoding.GetString(encodedBytes)
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace(':', '_')
                .Replace('"', '_')
                .Replace('/', '_')
                .Replace('|', '_')
                .Replace('?', '_')
                .Replace('*', '_');

            // During decompression of incorrectly-generated zip files,
            // Shift-JIS characters which encode to a representation that
            // includes a backslash result in the decompressor placing the files
            // in a subdirectory (or even subdirectories) based on characters
            // before and between all backslashes, and then names the file based
            // on the characters appearing after the final backslash. In these
            // cases, we're already parsing files in one of those generated
            // subdirectories and have only to deal with the file names having
            // been abbreviated. We can usually find such files in the
            // applicable directory via the substring after the final backslash.
            var lastIndexOfBackslash = decodedBeforeDirectoryRemoval.LastIndexOf('\\');
            return lastIndexOfBackslash == -1
                ? decodedBeforeDirectoryRemoval
                : decodedBeforeDirectoryRemoval.Substring(lastIndexOfBackslash + 1);
        }

        private static bool TryFindViaMainFileName(
            string directory,
            string mainFileName,
            string expectedCompanionPath,
            out string foundCompanionFileName)
        {
            var mainFilePath = Path.Combine(directory, mainFileName);

            var companionFileExtension = Path.GetExtension(expectedCompanionPath);

            var mainFilePathWithCompanionFileExtension =
                Path.ChangeExtension(mainFilePath, companionFileExtension);

            // Whether mangled or not, most companion files have names which
            // match the name of the main file, except for the difference in
            // the file extension. We can check for these by determining what
            // the file might be called when the extension is replaced with the
            // appropriate one and then check for the existence of that file.
            var mainFileNameWithCompanionFileExtension =
                Path.GetFileName(mainFilePathWithCompanionFileExtension);
            if (File.Exists(mainFilePathWithCompanionFileExtension))
            {
                Trace.TraceInformation(
                    $"{nameof(CDTXCompanionFileFinder)} could not find expected file '{expectedCompanionPath}' but found '{mainFileNameWithCompanionFileExtension}' by matching the '{mainFileName}' file name with the expected file extension.");

                foundCompanionFileName = mainFileNameWithCompanionFileExtension;
                return true;
            }

            foundCompanionFileName = null;
            return false;
        }

        private static bool TryFindViaCompanionFileExtension(
            string directory,
            string expectedCompanionPath,
            out string foundCompanionFileName)
        {
            var companionFileExtension = Path.GetExtension(expectedCompanionPath);

            if (string.IsNullOrEmpty(companionFileExtension))
            {
                Trace.TraceWarning(
                    $"{nameof(CDTXCompanionFileFinder)} could not find expected file '{expectedCompanionPath}' and could not search for appropriate sibling files because this file has no extension.");
            }
            else
            {
                // If no more precise approach can find the right file, we can
                // usually safely find it by looking for any file with the
                // expected file extension in the same folder as the main file.
                // However, if someone extracts a collection of songs into a
                // single folder, we will see many files with the expected
                // extension. Therefore, we will only treat the file as found
                // if there is one and only one file with the expected file
                // extension within in the directory in question.
                var filesWithTheCompanionFileExtension =
                    Directory.GetFiles(directory, "*" + companionFileExtension);
                if (filesWithTheCompanionFileExtension.Length == 1)
                {
                    var foundCompanionFilePath = filesWithTheCompanionFileExtension[0];
                    foundCompanionFileName = Path.GetFileName(foundCompanionFilePath);

                    Trace.TraceInformation(
                        $"{nameof(CDTXCompanionFileFinder)} could not find expected file '{expectedCompanionPath}' but found '{foundCompanionFileName}' by searching for a single sibling file with the expected extension.");

                    return true;
                }
            }

            foundCompanionFileName = null;
            return false;
        }
    }
}
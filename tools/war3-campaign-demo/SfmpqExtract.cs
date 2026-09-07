using System;
using System.IO;
using System.Runtime.InteropServices;

internal static class SfmpqExtract
{
    private const uint MaxEntrySize = 512u * 1024u * 1024u;

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string fileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr module, string procName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr module);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private delegate bool OpenArchive(string archiveName, uint priority, uint flags, out IntPtr archive);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private delegate bool OpenFileEx(IntPtr archive, string fileName, uint searchScope, out IntPtr file);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint GetFileSize(IntPtr file, out uint fileSizeHigh);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool ReadFile(IntPtr file, byte[] buffer, uint bytesToRead, out uint bytesRead, IntPtr overlapped);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool CloseFile(IntPtr file);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool CloseArchive(IntPtr archive);

    private static T GetExport<T>(IntPtr module, string name) where T : class
    {
        IntPtr address = GetProcAddress(module, name);
        if (address == IntPtr.Zero)
        {
            throw new InvalidOperationException("SFMPQ export not found: " + name);
        }

        return (T)(object)Marshal.GetDelegateForFunctionPointer(address, typeof(T));
    }

    public static int Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.Error.WriteLine("Usage: SfmpqExtract <sfmpq.dll> <archive> <entry> <output-file>");
            return 2;
        }

        string dllPath = Path.GetFullPath(args[0]);
        string archivePath = Path.GetFullPath(args[1]);
        string entryName = args[2];
        string outputPath = Path.GetFullPath(args[3]);

        if (!File.Exists(dllPath) || !File.Exists(archivePath))
        {
            Console.Error.WriteLine("DLL or archive not found.");
            return 2;
        }

        IntPtr module = LoadLibrary(dllPath);
        if (module == IntPtr.Zero)
        {
            Console.Error.WriteLine("LoadLibrary failed: " + Marshal.GetLastWin32Error());
            return 3;
        }

        IntPtr archive = IntPtr.Zero;
        IntPtr file = IntPtr.Zero;
        try
        {
            OpenArchive openArchive = GetExport<OpenArchive>(module, "SFileOpenArchive");
            OpenFileEx openFile = GetExport<OpenFileEx>(module, "SFileOpenFileEx");
            GetFileSize getFileSize = GetExport<GetFileSize>(module, "SFileGetFileSize");
            ReadFile readFile = GetExport<ReadFile>(module, "SFileReadFile");

            if (!openArchive(archivePath, 0, 0, out archive))
            {
                Console.Error.WriteLine("Cannot open archive: " + archivePath);
                return 4;
            }

            if (!openFile(archive, entryName, 0, out file))
            {
                Console.Error.WriteLine("Entry not found: " + entryName);
                return 5;
            }

            uint sizeHigh;
            uint size = getFileSize(file, out sizeHigh);
            if (sizeHigh != 0 || size == UInt32.MaxValue || size > MaxEntrySize)
            {
                Console.Error.WriteLine("Entry size is invalid or exceeds the 512 MiB safety limit.");
                return 6;
            }

            byte[] data = new byte[(int)size];
            uint bytesRead;
            if (size > 0 && (!readFile(file, data, size, out bytesRead, IntPtr.Zero) || bytesRead != size))
            {
                Console.Error.WriteLine("Could not read the complete entry.");
                return 7;
            }

            string parent = Path.GetDirectoryName(outputPath);
            if (!String.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.WriteAllBytes(outputPath, data);
            Console.WriteLine(size.ToString());
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 8;
        }
        finally
        {
            try
            {
                if (file != IntPtr.Zero)
                {
                    GetExport<CloseFile>(module, "SFileCloseFile")(file);
                }
            }
            catch
            {
            }

            try
            {
                if (archive != IntPtr.Zero)
                {
                    GetExport<CloseArchive>(module, "SFileCloseArchive")(archive);
                }
            }
            catch
            {
            }

            FreeLibrary(module);
        }
    }
}

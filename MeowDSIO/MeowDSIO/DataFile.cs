using MeowDSIO.DataFiles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO
{
    public abstract class DataFile : INotifyPropertyChanged
    {
        public event EventHandler IsModifiedChanged;

        protected virtual void OnIsModifiedChanged()
        {
            IsModifiedChanged?.Invoke(this, EventArgs.Empty);
        }

        //TODO: Make use of IsModified on every type of DataFile.
        private bool _isModified = false;
        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    RaisePropertyChanged();
                    OnIsModifiedChanged();
                }

            }
        }

        protected abstract void Read(DSBinaryReader bin, IProgress<(int, int)> prog);
        protected abstract void Write(DSBinaryWriter bin, IProgress<(int, int)> prog);

        private string _filePath = null;
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FileBackupPath));
            }
        }

        private string _virtualUri = null;
        public string VirtualUri
        {
            get => _virtualUri;
            set
            {
                _virtualUri = value;
                RaisePropertyChanged();
            }
        }

        public string FileBackupPath
        {
            get
            {
                if (FilePath == null)
                    return null;

                return FilePath + ".bak";
            }
        }

        /// <summary>
        /// Checks if a backup exists.
        /// </summary>
        /// <returns>Null if FilePath is null, True if backup exists, etc.</returns>
        public bool? CheckBackupExist()
        {
            if (FilePath == null)
                return null;

            return File.Exists(FileBackupPath);
        }

        /// <summary>
        /// Creates backup.
        /// </summary>
        /// <param name="overwriteExisting">If this is false, no new backups will be created if one already exists.
        /// This preserves the initial backup, preventing any changes from ocurring to it.</param>
        /// <returns>True if backup is saved, False if no backup is saved to preserve existing, Null if FilePath == Null.</returns>
        public bool? CreateBackup(bool overwriteExisting = false)
        {
            if (FilePath == null)
                return null;

            if (overwriteExisting || CheckBackupExist() == false)
            {
                File.Copy(FilePath, FileBackupPath);
                return true;
            }
            else
            {
                return false;
            }
        }

        public DateTime? GetBackupDate()
        {
            if (FilePath == null)
                return null;

            return File.GetCreationTime(FileBackupPath);
        }

        /// <summary>
        /// Overwrites the file with a previously-created backup.
        /// </summary>
        /// <returns>True if backup restored, False if no backup exists, Null if FilePath is Null</returns>
        public bool? RestoreBackup()
        {
            if (FilePath == null)
                return null;

            if (CheckBackupExist() == true)
            {
                File.Copy(FileBackupPath, FilePath, overwrite: true);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void ReloadDcx<T>(T data, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            if (data.FilePath == null)
            {
                throw new InvalidOperationException($"Data file cannot be reloaded unless it was " +
                    $"previously saved to or loaded from a file and had its {nameof(FilePath)} property set.");
            }

            using (var fileStream = File.Open(data.FilePath, FileMode.Open))
            {
                using (var binaryReader = new DSBinaryReader(data.FilePath ?? data.VirtualUri, fileStream))
                {
                    DCX dcx = new DCX();
                    dcx.FilePath = data.FilePath;
                    dcx.VirtualUri = data.VirtualUri;
                    dcx.Read(binaryReader, prog);

                    using (var tempStream = new MemoryStream(dcx.Data))
                    {
                        using (var dcxBinaryReader = new DSBinaryReader(data.FilePath ?? data.VirtualUri, tempStream))
                        {
                            data.Read(dcxBinaryReader, prog);
                            data.IsModified = false;
                        }
                    }

                }
            }
        }

        public static void Reload<T>(T data, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            if (data.FilePath == null)
            {
                throw new InvalidOperationException($"Data file cannot be reloaded unless it was " +
                    $"previously saved to or loaded from a file and had its {nameof(FilePath)} property set.");
            }

            using (var fileStream = File.Open(data.FilePath, FileMode.Open))
            {
                using (var binaryReader = new DSBinaryReader(data.FilePath, fileStream))
                {
                    data.Read(binaryReader, prog);
                    data.IsModified = false;
                }
            }
        }

        public static void ResaveDcx<T>(T data, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            var dcx = new DCX()
            {
                Data = SaveAsBytes<T>(data, data.FilePath ?? data.VirtualUri, prog),
                FilePath = data.FilePath,
                VirtualUri = data.VirtualUri
            };
            Resave<DCX>(dcx, prog);
        }

        public static void Resave<T>(T data, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            if (data.FilePath == null)
            {
                throw new InvalidOperationException($"Data file cannot be resaved unless it was " +
                    $"previously saved to or loaded from a file and had its {nameof(FilePath)} property set.");
            }

            //Should no longer save a file with 0 bytes in it if it gets an exception during write oops
            var newBytes = DataFile.SaveAsBytes(data, data.FilePath, prog);

            using (var fileStream = File.Open(data.FilePath, FileMode.OpenOrCreate))
            {
                fileStream.Position = 0;
                fileStream.SetLength(0);
                using (var binaryWriter = new DSBinaryWriter(data.FilePath, fileStream))
                {
                    binaryWriter.Write(newBytes);
                }
            }
        }

        //public static T LoadFromDs3EncDcxFile<T>(string filePath, IProgress<(int, int)> prog = null)
        //    where T : DataFile, new()
        //{
        //    var dcx = LoadFromFile<DCX>(filePath);
        //    var data = LoadFromBytes<T>(dcx.Data, filePath, prog);
        //    data.FilePath = filePath;
        //    if (data.FilePath.ToUpper().EndsWith(".DCX"))
        //    {
        //        data.VirtualUri = data.FilePath.Substring(0, data.FilePath.Length - ".DCX".Length);
        //    }
        //    return data;
        //}

        public static T LoadFromDcxFile<T>(string filePath, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            var dcx = LoadFromFile<DCX>(filePath);
            var data = LoadFromBytes<T>(dcx.Data, filePath, prog);
            data.FilePath = filePath;
            if (data.FilePath.ToUpper().EndsWith(".DCX"))
            {
                data.VirtualUri = data.FilePath.Substring(0, data.FilePath.Length - ".DCX".Length);
            }
            return data;
        }


        public static T LoadFromFile<T>(string filePath, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            using (var fileStream = File.Open(filePath, FileMode.Open))
            {
                using (var binaryReader = new DSBinaryReader(filePath, fileStream))
                {
                    T result = new T();
                    result.FilePath = filePath;
                    result.Read(binaryReader, prog);
                    result.IsModified = false;
                    return result;
                }
            }
        }

        public static void SaveToDcxFile<T>(T data, string filePath, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            var bytes = SaveAsBytes<T>(data, filePath, prog);
            SaveToFile<DCX>(new DCX()
            {
                Data = bytes,
                FilePath = data.FilePath,
                VirtualUri = data.VirtualUri
            }, filePath, prog);
        }

        public static void SaveToFile<T>(T data, string filePath, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            //Should no longer save a file with 0 bytes in it if it gets an exception during write oops
            var newBytes = DataFile.SaveAsBytes(data, data.FilePath, prog);

            using (var fileStream = File.Open(filePath, FileMode.OpenOrCreate))
            {
                fileStream.Position = 0;
                fileStream.SetLength(0);
                using (var binaryWriter = new DSBinaryWriter(filePath, fileStream))
                {
                    binaryWriter.Write(newBytes);
                }
            }
        }

        public static T LoadFromDcxBytes<T>(byte[] bytes, string virtualUri, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            var dcx = LoadFromBytes<DCX>(bytes, virtualUri, prog);
            return LoadFromBytes<T>(dcx.Data, virtualUri, prog);
        }

        public static T LoadFromBytes<T>(byte[] bytes, string virtualUri, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            using (var tempStream = new MemoryStream(bytes))
            {
                using (var binaryReader = new DSBinaryReader(virtualUri, tempStream))
                {
                    T result = new T();
                    result.VirtualUri = virtualUri;
                    result.Read(binaryReader, prog);
                    result.IsModified = false;
                    return result;
                }
            }
        }

        public static byte[] SaveAsDcxBytes<T>(T data, string virtualUri, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            var dcx = new DCX()
            {
                Data = SaveAsBytes<T>(data, virtualUri, prog),
                FilePath = data.FilePath,
                VirtualUri = data.VirtualUri
            };
            return SaveAsBytes<DCX>(dcx, virtualUri, prog);
        }

        public static byte[] SaveAsBytes<T>(T data, string virtualUri, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            using (var tempStream = new MemoryStream())
            {
                tempStream.Position = 0;
                tempStream.SetLength(0);

                using (var binaryWriter = new DSBinaryWriter(null, tempStream))
                {
                    data.VirtualUri = virtualUri;
                    data.Write(binaryWriter, prog);
                    var result = tempStream.ToArray();
                    data.IsModified = false;
                    return result;
                }
            }
        }

        public static T LoadFromDcxStream<T>(Stream stream, string virtualUri, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            var dcx = LoadFromStream<DCX>(stream, virtualUri, prog);
            return LoadFromBytes<T>(dcx.Data, virtualUri, prog);
        }

        public static T LoadFromStream<T>(Stream stream, string virtualUri, IProgress<(int, int)> prog = null)
            where T : DataFile, new()
        {
            using (var binaryReader = new DSBinaryReader(virtualUri, stream))
            {
                T result = new T();
                result.VirtualUri = virtualUri;
                result.Read(binaryReader, prog);
                result.IsModified = false;
                return result;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string caller = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
    }
}

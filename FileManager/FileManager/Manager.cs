using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileManager
{
    internal class Manager
    {
        //хранение данных о файлах и каталогах
        private List<ObjectFileSystem> _objectsFileSystem;
        private int _depth;
        private Dictionary<string, int> _extension;
        private int _countFiles;
        private int _countDictionaries;
        private string _parentDictionary;
        long _size;

        //конструктор
        public Manager(string currentDirectory, int depth)
        {
            _objectsFileSystem = new List<ObjectFileSystem>();            
            //первый элемент - родительский каталог
            if (currentDirectory == "C:\\")
            {
                _objectsFileSystem.Add(new ObjectFileSystem(":", ObjectFileSystemType.Catalog, "", -1, currentDirectory));
            }
            else
            {
                DirectoryInfo df = new DirectoryInfo(currentDirectory);
                _objectsFileSystem.Add(new ObjectFileSystem(":", ObjectFileSystemType.Catalog, "", -1, df.Parent.FullName));
            }
            _extension = new Dictionary<string, int>();
            _depth = depth;
            _parentDictionary = currentDirectory;
            _size = 0;
            SearchDirectory(currentDirectory, _objectsFileSystem, depth);
        }
       
        public List<ObjectFileSystem> ObjectsFileSystem { get { return _objectsFileSystem; } }
        public int CountFiles { get { return _countFiles; } }
        public int CountDictionaries { get { return _countDictionaries; } }
        public int Depth { get { return _depth; } }
        public Dictionary<string, int> Extension { get { return _extension; } }
        public long Size
        {
            get { return _size; }
            set { _size = value; }
        }
        public string ParentDictionary { get { return _parentDictionary; } }
        public int Count()
        {
            return _objectsFileSystem.Count;
        }

        //сбор информации по каталогам и файлам
        private void SearchDirectory(string currentDirectory, List<ObjectFileSystem> objectsFileSystem, int depth)
        {
            //глубина рекурсии промотра каталогов
            depth--;
            if (depth < 0)
            {
                return;
            }

            try
            {
                foreach (var directory in new DirectoryInfo(currentDirectory).GetDirectories())
                {
                    //сохраняем информацию заданной глубины на вывод на экран
                    objectsFileSystem.Add(new ObjectFileSystem(directory.Name, ObjectFileSystemType.Catalog, directory.CreationTime.ToString(), depth, directory.FullName));
                    //обход всей рекурсии для сбора информации о количестве файлов
                    SearchDirectory(directory.FullName, objectsFileSystem, depth);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Search. Src:" + currentDirectory + " ERROR: " + ex.Message;
                ErrorMessage.WriteErrorToFile(errorMessage);
            }
            try
            {
                //собираем информацию по родительскому каталогу
                if (currentDirectory == _parentDictionary)
                {
                    string[] directories = Directory.GetDirectories(currentDirectory);
                    _countDictionaries = directories.Count();
                    string[] files = Directory.GetFiles(currentDirectory);
                    _countFiles = files.Count();
                    //находим все расширения в каталоге
                    var extensions =
                       from file in files
                       group file by Path.GetExtension(file);
                    //собираем информацию по расширениям
                    foreach (var extension in extensions)
                    {
                        if (_extension.ContainsKey(extension.Key))
                        {
                            _extension[extension.Key] += extension.Count();
                        }
                        else
                        {
                            _extension[extension.Key] = extension.Count();
                        }
                    }
                }

                foreach (var file in new DirectoryInfo(currentDirectory).GetFiles())
                {
                    objectsFileSystem.Add(new ObjectFileSystem(file.Name, ObjectFileSystemType.File, file.CreationTime.ToString(), depth, file.Length, file.Extension, currentDirectory));
                    if (currentDirectory == _parentDictionary)
                    {
                        Size += file.Length;
                    }

                }
            }
            catch (Exception ex)
            {
                string errorMessage = "Search. Src:" + currentDirectory + " ERROR: " + ex.Message;
                ErrorMessage.WriteErrorToFile(errorMessage);
            }

        }
        //копирование файла
        private string CopyFile(string sourcePath, string destPath)
        {
            string status = "";
            try
            {
                File.Copy(sourcePath, destPath, overwrite: true);
                status = "Копирование завершено успешно.";
            }
            catch (Exception ex)
            {
                status = $"Копирование завершено с ошибкой:" + ex.Message;
                string errorMessage = "Copy. Src:" + sourcePath + " Dest:" + destPath + " ERROR: " + ex.Message;
                ErrorMessage.WriteErrorToFile(errorMessage);
            }
            return status;
        }

        //копирование каталога
        private string CopyDirectory(string sourcePath, string destPath)
        {

            string status = "";

            string[] files = Directory.GetFiles(sourcePath);
            string[] directories = Directory.GetDirectories(sourcePath);

            //копирование файлов
            foreach (var file in files)
            {
                // Получаем имя файла
                string fName = file.Substring(sourcePath.Length + 1);
                try
                {
                    // копируем
                    File.Copy(file, Path.Combine(destPath, fName));
                }
                catch (Exception ex)
                {
                    status = ex.Message;
                    return status;
                }
            }

            //копирование директорий
            foreach (var pathSource in directories)
            {
                // получаем имя директории
                string dName = pathSource.Substring(sourcePath.Length + 1);
                string pathDest = Path.Combine(destPath, dName);
                try
                {
                    // создаем каталог
                    Directory.CreateDirectory(pathDest);
                }
                catch (Exception ex)
                {
                    status = ex.Message;
                    // TODO: error in CopyDirectory
                    return status;
                }
                CopyDirectory(pathSource, pathDest);
            }

            status = "Копирование каталога завершено успешно.";
            return status;
        }

        //удаление файла
        private string RemoveFile(string path)
        {
            string status = "";
            try
            {
                File.Delete(path);
                status = "Удаление файла завершено успешно.";
            }
            catch (Exception ex)
            {
                status = $"Удаление файла завершено с ошибкой:" + ex.Message;
                string errorMessage = "Remove. Src:" + path +  " ERROR: " + ex.Message;
                ErrorMessage.WriteErrorToFile(errorMessage);
            }
            return status;
        }

        //удаление каталога
        private string RemoveDirectory(string path)
        {
            string status;
            try
            {
                Directory.Delete(path, recursive: true);
                status = "Удаление каталога завершено успешно.";
            }
            catch (Exception ex)
            {
                status = $"Удаление завершено с ошибкой:" + ex.Message;
                string errorMessage = "Remove. Src:" + path + " ERROR: " + ex.Message;
                ErrorMessage.WriteErrorToFile(errorMessage);
            }
            return status;
        }


        //парсинг и верификация заданной команды
        public SplitedCommand SplitCmd(string cmd)
        {
            SplitedCommand command = new SplitedCommand();
            //разбиение комманды
            //добавляем команду
            string pattern1param = @"^cp|ls|rm ";
            if (Regex.IsMatch(cmd, pattern1param))
            {
                command.cmd = Regex.Match(cmd, pattern1param).ToString().Trim();
                cmd = Regex.Replace(cmd, pattern1param, "");
            }
            else
            {
                command.errorMessage = "Ошибка. Введена неверная команда.";
                command.isValid = false;
                string errorMessage = "Split. ERROR:Uncorrect command";
                ErrorMessage.WriteErrorToFile(errorMessage);
                return command;
            }
            //проверяем второй параметр
            Regex innerRegex = new Regex("\"[^\"]*\"");
            if (innerRegex.IsMatch(cmd))
            {
                command.pathSrc = innerRegex.Match(cmd).ToString().Replace("\"", "");
                cmd = innerRegex.Replace(cmd, "", 1);
                cmd = cmd.Trim();
                //проверка корректности
                if (Path.HasExtension(command.pathSrc))
                {
                    if (!File.Exists(command.pathSrc))
                    {
                        command.errorMessage = "Ошибка. Данного файла не существует.";
                        string errorMessage = "Split. ERROR: File don`t exist";
                        ErrorMessage.WriteErrorToFile(errorMessage);
                        command.isValid = false;
                        return command;
                    }
                    else
                    {
                        command.type = ObjectFileSystemType.File;
                    }
                }
                //если каталог...
                else
                {
                    if (!Directory.Exists(command.pathSrc))
                    {
                        command.errorMessage = "Ошибка. Данного каталога не существует.";
                        string errorMessage = "Split. ERROR: Directory don`t exist";
                        ErrorMessage.WriteErrorToFile(errorMessage);
                        command.isValid = false;
                        return command;
                    }
                    else
                    {
                        command.type = ObjectFileSystemType.Catalog;
                    }
                }
            }
            else
            {
                command.errorMessage = "Ошибка. Введен неверный второй параметр.";
                command.isValid = false;
                string errorMessage = "Split. ERROR: Uncorrect command";
                ErrorMessage.WriteErrorToFile(errorMessage);
                return command;
            }
            //проверяем третий параметр
            //случай если путь к каталогу
            if (innerRegex.IsMatch(cmd) && command.cmd == "cp" && command.type == ObjectFileSystemType.Catalog)
            {
                command.pathDst = innerRegex.Match(cmd).ToString().Replace("\"", "");
                cmd = innerRegex.Replace(cmd, "");
                cmd = cmd.Trim();
            }
            else
            //случай педжинга
            {
                string pattern3param = @"^-p\d+";
                if (Regex.IsMatch(cmd, pattern3param) && command.cmd == "ls")
                {
                    string param = Regex.Match(cmd, pattern3param).ToString().Replace("-p", "");
                    if (!int.TryParse(param, out command.page))
                    {
                        command.errorMessage = "Ошибка. Превышение допустимого значения страницы.";
                        command.isValid = false;
                        string errorMessage = "Split. ERROR: Uncorrect command";
                        ErrorMessage.WriteErrorToFile(errorMessage);
                        return command;
                    }
                    cmd = Regex.Replace(cmd, pattern3param, "");
                    cmd = cmd.Trim();
                }
                else
                {
                    if (((command.cmd == "cp" && command.type == ObjectFileSystemType.File) || command.cmd == "rm")
                       && string.IsNullOrEmpty(cmd.ToString()))
                    {
                        command.isValid = true;
                        return command;
                    }
                    command.errorMessage = "Ошибка. Введен неверный третий параметр.";
                    string errorMessage = "Split. ERROR: Uncorrect command";
                    ErrorMessage.WriteErrorToFile(errorMessage);
                    command.isValid = false;
                    return command;
                }
            }
            //если что-то осталось
            if (!string.IsNullOrEmpty(cmd))
            {
                command.errorMessage = "Ошибка. Неверное число параметров.";
                string errorMessage = "Split. ERROR: Uncorrect command";
                ErrorMessage.WriteErrorToFile(errorMessage);
                command.isValid = false;
                return command;
            }
            command.isValid = true;
            return command;

        }

        //выполнение команды
        public string ExecuteCommand(SplitedCommand splCommand)
        {
            string status = string.Empty;
            switch (splCommand.cmd)
            {
                case "ls":
                    {
                        if (splCommand.type == ObjectFileSystemType.Catalog)
                        {
                            CreateNewObjectsFileSystem(splCommand.pathSrc);
                            return "Успешно";
                        }
                        //если попытка открыть файл вместо каталога
                        else
                        {
                            string errorMessage = "Execute. ERROR:Uncorrect command";
                            ErrorMessage.WriteErrorToFile(errorMessage);
                            return "Введите правильный путь к каталогу";
                        }
                    }
                case "cp":
                    {
                        if (splCommand.type == ObjectFileSystemType.File)
                        {
                            if (string.IsNullOrEmpty(splCommand.pathDst))
                            {

                                splCommand.pathDst = Path.Combine(Path.GetDirectoryName(splCommand.pathSrc), (Path.GetFileNameWithoutExtension(splCommand.pathSrc) + "_copy"
                                    + Path.GetExtension(splCommand.pathSrc)));
                            }
                            status = CopyFile(splCommand.pathSrc, splCommand.pathDst);
                            return status;
                        }
                        else
                        {
                            if (!Directory.Exists(splCommand.pathDst))
                            {
                                Directory.CreateDirectory(splCommand.pathDst);
                            }
                            status = CopyDirectory(splCommand.pathSrc, splCommand.pathDst);
                            return status;
                        }
                    }
                case "rm":
                    {
                        if (splCommand.type == ObjectFileSystemType.File)
                        {
                            status = RemoveFile(splCommand.pathSrc);
                            return status;
                        }
                        else
                        {
                            status = RemoveDirectory(splCommand.pathSrc);
                            return status;
                        }
                    }
            }
            return status;
        }

        //метод пересоздает новый список, от родительского каталога
        public void CreateNewObjectsFileSystem(string currentDirectory)
        {
            _objectsFileSystem.Clear();
            _extension.Clear();
            _countFiles = 0;
            _countDictionaries = 0;
            _size = 0;
            _parentDictionary = currentDirectory;
            if (currentDirectory == "C:\\")
            {
                _objectsFileSystem.Add(new ObjectFileSystem(":", ObjectFileSystemType.Catalog, "", -1, currentDirectory));
            }
            else
            {
                DirectoryInfo df = new DirectoryInfo(currentDirectory);
                _objectsFileSystem.Add(new ObjectFileSystem(":", ObjectFileSystemType.Catalog, "", -1, df.Parent.FullName));
            }
            SearchDirectory(currentDirectory, _objectsFileSystem, Depth);
        }
    }
}

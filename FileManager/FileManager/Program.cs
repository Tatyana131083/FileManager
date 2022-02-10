using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace FileManager
{
    //структура для хранения введенной команды
    struct SplitedCommand
    {
        public string cmd;
        public string pathSrc;
        public string pathDst;
        public int page;
        public bool isValid;
        public string errorMessage;
        public ObjectFileSystemType type;

    }
    internal class Program
    {
        //констаны по ширине колонок на консоле
        const int WidthClmnName = 40;
        const int WidthClmnData = 25;
        const int WidthClmnDType = 12;
        const int WidthClmnSize = 15;
        const int WidthClmnEx = 10;
        const int Indent = 4;
        const int LevelMax = 2;


        static void Main(string[] args)
        {


            int choiceString = 0;
            int currentPage = 1;
            int stringsOnPage = Properties.Settings.Default.StringCount;
            string rootPath = "";
            // создание файла для записи директории
            string path = Path.Combine(Environment.CurrentDirectory, "directory.xml");
            XmlSerializer xs = new XmlSerializer(rootPath.GetType());
            using (FileStream xmlLoad = File.Open(path, FileMode.Open))
            {
                // десериализация
                rootPath = xs.Deserialize(xmlLoad) as string;
                if (string.IsNullOrEmpty(rootPath))
                {
                    rootPath = "C:\\";
                }
            }

            string status = "";
            //список для хранения истории команд
            List<string> historyCmd = new List<string>();
            int historyCounter = 0;
            Manager fm = new Manager(rootPath, LevelMax);
            ConsoleKeyInfo userChoice;
            StringBuilder cmd = new StringBuilder();
            PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, "");

            do
            {

                userChoice = Console.ReadKey();
                switch (userChoice.Key)
                {
                    case ConsoleKey.DownArrow:
                        {
                            if ((((currentPage - 1) * stringsOnPage) + choiceString) < fm.Count() - 1)
                            {
                                if (choiceString == stringsOnPage - 1)
                                {
                                    currentPage++;
                                    choiceString = 0;
                                    PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, "");
                                }
                                else
                                {
                                    choiceString++;
                                    PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, "");
                                }
                            }
                            break;

                        }
                    case ConsoleKey.UpArrow:
                        {
                            if (choiceString == 0 && currentPage == 1)
                            {
                                break;
                            }
                            else
                            {
                                if (choiceString == 0)
                                {
                                    currentPage--;
                                    choiceString = stringsOnPage - 1;
                                    PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, "");
                                }
                                else
                                {
                                    choiceString--;
                                    PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, "");
                                }
                                break;
                            }
                        }
                    case ConsoleKey.LeftArrow:
                        {
                            if (currentPage == 1)
                            {
                                break;
                            }
                            else
                            {
                                currentPage--;
                                choiceString = 0;
                                PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, status);
                            }
                            break;

                        }
                    case ConsoleKey.RightArrow:
                        {
                            //проверка на последнюю страницу
                            if (currentPage == Math.Ceiling(fm.Count() / (stringsOnPage * 1.0)))
                            {
                                break;
                            }
                            else
                            {
                                currentPage++;
                                choiceString = 0;
                                PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, status);
                            }
                            break;

                        }
                    case ConsoleKey.Backspace:
                        {
                            if (cmd.Length > 0)
                            {
                                cmd.Remove(cmd.Length - 1, 1);
                                PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, "");
                                break;
                            }
                            Console.CursorLeft = 18;
                            break;
                        }
                    case ConsoleKey.Enter:
                        {
                            //вход в активный каталог, если командная строка пуста
                            if (cmd.Length == 0)
                            {
                                if (fm.ObjectsFileSystem[choiceString].Type == ObjectFileSystemType.Catalog)
                                {
                                    rootPath = fm.ObjectsFileSystem[choiceString].AbsPath;
                                    fm.CreateNewObjectsFileSystem(rootPath);
                                    choiceString = 0;
                                    currentPage = 1;
                                    PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, status);
                                }

                            }
                            //а если не пустая, то выполнение командной строки
                            else
                            {
                                SplitedCommand splCommand = new SplitedCommand();
                                splCommand = fm.SplitCmd(cmd.ToString());
                                if (splCommand.isValid == true)
                                {
                                    status = fm.ExecuteCommand(splCommand);
                                    if (splCommand.cmd == "ls")
                                    {
                                        currentPage = splCommand.page;
                                        choiceString = 0;
                                    }

                                }
                                else
                                {
                                    status = splCommand.errorMessage;
                                }
                                if(historyCmd.Count == 10)
                                {
                                    historyCmd.RemoveAt(0);
                                }
                                historyCmd.Add(cmd.ToString());
                                cmd.Clear();
                                PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, status);
                            }
                            break;

                        }

                    case ConsoleKey.PageUp:
                        {
                           
                            if(historyCounter > 0)
                            {
                                cmd.Clear();
                                historyCounter--;
                                cmd.Append(historyCmd[historyCounter]);                                                                
                                PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, status);
                            }
                            else
                            {
                                Console.CursorLeft--;
                            }
                            break;
                        }

                    case ConsoleKey.PageDown:
                        {

                            if (historyCounter < historyCmd.Count)
                            {
                                cmd.Clear();
                                cmd.Append(historyCmd[historyCounter]);
                                historyCounter++;
                                PrintScreen(fm, cmd, choiceString, currentPage, stringsOnPage, status);
                            } else
                            {
                                Console.CursorLeft--;
                            }
                            break;
                        }

                    default: cmd.Append(userChoice.KeyChar); break;
                }
            } while (userChoice.Key != ConsoleKey.Escape);
            //завершающие операции            
            using (FileStream stream = File.Create(path))
            {
                // cериализация
                xs.Serialize(stream, fm.ParentDictionary);
            }

        }

        //Вывод области с каталогами
        static void PrintDirectory(List<ObjectFileSystem> objectsFileSystem, int choiceString, int currentPage, int stringsOnPage)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.Clear();
            int index = 0;
            int startPosition = (currentPage - 1) * stringsOnPage;
            //проверка индекса
            int endPosition = startPosition + stringsOnPage;
            int emptyString = 0;
            if (endPosition > objectsFileSystem.Count)
            {
                emptyString = stringsOnPage;
                stringsOnPage = objectsFileSystem.Count % stringsOnPage;
                emptyString -= stringsOnPage;
            }
            PrintLine();
            Console.WriteLine($"{" ",Indent}{"Имя",-WidthClmnName}{"Дата создания",-WidthClmnData}{"Тип",-WidthClmnDType}{"Размер",-WidthClmnSize}{"Расширение",-WidthClmnEx}");
            PrintLine();
            foreach (ObjectFileSystem obj in objectsFileSystem.GetRange(startPosition, stringsOnPage))
            {
                int count = Math.Abs(obj.Level - LevelMax + 1);
                int widthName = WidthClmnName - count * 4;

                StringBuilder objName = new StringBuilder();
                for (int i = 0; i < count * 4; i++)
                {
                    objName.Append(" ");
                }
                //обрезание имени под колонку
                objName.Append(obj.Name.Length <= widthName - 4 ? obj.Name : obj.Name.Substring(0, widthName - 4));
                if (choiceString == index)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                }
                if(index == 0)
                {
                    Console.WriteLine($"{ "   :",-WidthClmnName}");
                } else if (obj.Type == ObjectFileSystemType.Catalog)
                {
                    Console.WriteLine($"{" ",Indent}{objName,-WidthClmnName}{obj.CreationTime,-WidthClmnData}{"Каталог"}");
                }
                else
                {
                    Console.WriteLine($"{" ",Indent}{objName,-WidthClmnName}{obj.CreationTime,-WidthClmnData}{"Файл",-WidthClmnDType}{obj.Size,-WidthClmnSize}{obj.Extension}");
                }

                if (choiceString == index)
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                }
                index++;
            }
            while (emptyString > 0)
            {
                Console.WriteLine();
                emptyString--;
            }
            PrintLine();
        }
        //Вывол горизонтальной линии
        static void PrintLine()
        {
            int count = Indent + WidthClmnName + WidthClmnData + WidthClmnDType + WidthClmnSize + WidthClmnEx;
            while (count > 0)
            {
                Console.Write("-");
                count--;
            }
            Console.WriteLine();
        }
        //Вывод области с командной строкой
        static void PrintCmd(StringBuilder cmd)
        {
            if (cmd.Length == 0)
            {
                Console.Write("Командная строка: ");
            }
            else
            {
                Console.Write("Командная строка: ");
                Console.Write(cmd);
            }
        }
        //Вывод статуса исполнения команд
        static void PrintStatus(string status)
        {
            Console.Write("Статус выполнения: ");
            Console.WriteLine(status);
            PrintLine();
        }
        //Вывод информации о каталоге
        static void PrintInfo(Manager fm)
        {
            Console.WriteLine($"Краткая информация: ");
            Console.WriteLine($"Кол-во подкаталогов: {fm.CountDictionaries}");
            Console.WriteLine($"Кол-во файлов: {fm.CountFiles}   {fm.Size} Б");
            int i = 0;
            foreach (var current in fm.Extension.Keys)
            {
                if (i > 5)
                {
                    Console.WriteLine();
                    i = 0;
                }
                Console.Write($"    {fm.Extension[current]} {current}    |   ");
                i++;
            }
            Console.WriteLine();
            PrintLine();
        }
        //Вывод на консоль
        static void PrintScreen(Manager fm, StringBuilder cmd, int choiceString, int currentPage, int stringsOnPage, string status)
        {
            PrintDirectory(fm.ObjectsFileSystem, choiceString, currentPage, stringsOnPage);
            PrintInfo(fm);
            PrintStatus(status);
            PrintCmd(cmd);
        }
 

 

    }
}

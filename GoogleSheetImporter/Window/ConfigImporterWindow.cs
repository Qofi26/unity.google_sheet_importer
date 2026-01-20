using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleSheetImporter.Mappers;
using GoogleSheetImporter.Parsers;
using GoogleSheetImporter.Services;
using GoogleSheetImporter.Settings;
using GoogleSheetImporter.Wrappers;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GoogleSheetImporter.Window
{
    internal sealed class ConfigImporterWindow : EditorWindow
    {
        private ImportSettings _settings;
        private IAuthSettings _authSettings;
        private GoogleAuthService _auth;
        private ReorderableList _mappersList;
        private bool _showPrivate;

        private List<GDriveFolder> Folders => _settings.Folders;
        private Dictionary<string, bool> SelectedFiles => _settings.SelectedFiles;
        private List<GDriveFile> Spreadsheets => _settings.Spreadsheets;

        private int SelectedFolderIndex
        {
            get => _settings.SelectedFolderIndex;
            set => _settings.SelectedFolderIndex = value;
        }

        private Vector2 _scroll;

        [MenuItem("Tools/Google Drive Sheets Importer...")]
        private static void Open()
        {
            var win = GetWindow<ConfigImporterWindow>("Sheets Importer");
            win.minSize = new Vector2(680, 520);
            win.Show();
        }

        private void OnEnable()
        {
            _settings = ImportSettings.Load();
            _authSettings = GoogleSheetImporterSettings.Instance;
            _auth = new GoogleAuthService(_authSettings);
            InitializeMappersList();
        }

        private void OnDisable()
        {
            _settings?.Save();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawAuth();

            EditorGUILayout.Space(10);

            DrawSource();

            EditorGUILayout.Space(10);

            DrawOutput();

            using (new EditorGUI.DisabledScope(Spreadsheets.Count == 0))
            {
                if (GUILayout.Button("Импортировать выбранные таблицы в JSON"))
                {
                    if (Authorize())
                    {
                        ImportSelected();
                    }
                }
            }

            DrawApply();

            EditorGUILayout.EndScrollView();
        }

        private void LoadSubfolders()
        {
            try
            {
                var drive = _auth.CreateDriveService();
                var wrapper = new DriveServiceWrapper(drive);

                var id = DriveServiceWrapper.ExtractFolderId(_settings.RootFolderUrlOrId);
                Folders.Clear();
                Folders.AddRange(wrapper.ListSubfolders(id, _settings.NamePrefix));
                SelectedFolderIndex = Folders.Count > 0
                    ? 0
                    : -1;
                Spreadsheets.Clear();
                SelectedFiles.Clear();

                ShowNotification(new GUIContent($"Найдено папок: {Folders.Count}"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ShowNotification(new GUIContent("Ошибка загрузки папок (см. Console)"));
            }
        }

        private void LoadSpreadsheets()
        {
            try
            {
                if (SelectedFolderIndex < 0)
                {
                    ShowNotification(new GUIContent("Выберите папку"));
                    return;
                }

                var drive = _auth.CreateDriveService();
                var wrapper = new DriveServiceWrapper(drive);
                var folderId = Folders[SelectedFolderIndex].Id;

                Spreadsheets.Clear();
                Spreadsheets.AddRange(wrapper.ListSpreadsheetsInFolder(folderId));
                SelectedFiles.Clear();
                foreach (var f in Spreadsheets) SelectedFiles[f.Id] = false;

                ShowNotification(new GUIContent($"Найдено таблиц: {Spreadsheets.Count}"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ShowNotification(new GUIContent("Ошибка загрузки таблиц (см. Console)"));
            }
        }

        private void DrawAuth()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Авторизация", EditorStyles.boldLabel);

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                _showPrivate = !_showPrivate;
            }

            EditorGUILayout.EndHorizontal();

            if (_showPrivate)
            {
                _authSettings.Mode = (AuthMode) EditorGUILayout.EnumPopup("Auth Mode", _authSettings.Mode);

                if (_authSettings.Mode == AuthMode.OAuthInstalledApp)
                {
                    EditorGUILayout.BeginVertical();
                    _authSettings.ClientId = EditorGUILayout.TextField("client_id", _authSettings.ClientId);
                    _authSettings.ClientSecret = EditorGUILayout.TextField("client_secret", _authSettings.ClientSecret);
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    _authSettings.ServiceAccountKeyPath = EditorGUILayout.TextField("service_account_key.json",
                        _authSettings.ServiceAccountKeyPath);
                    if (GUILayout.Button("...", GUILayout.Width(30)))
                    {
                        var path = EditorUtility.OpenFilePanel("Выберите service_account_key.json", "", "json");
                        if (!string.IsNullOrEmpty(path)) _authSettings.ServiceAccountKeyPath = path;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox(
                        "Важно: Поделитесь Root папкой (или нужными подпапками/файлами) с email сервис-аккаунта, иначе доступ будет запрещён.",
                        MessageType.Info);
                }
            }

            if (GUILayout.Button("Authorize"))
            {
                Authorize();
            }
        }

        private bool Authorize()
        {
            try
            {
                _auth.Authorize();
                ShowNotification(new GUIContent("Authorized"));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ShowNotification(new GUIContent("Auth failed, смотри Console"));
                return false;
            }
        }

        private void DrawSource()
        {
            EditorGUILayout.LabelField("Параметры источника", EditorStyles.boldLabel);

            _settings.RootFolderUrlOrId = EditorGUILayout.TextField("Root Folder URL/ID", _settings.RootFolderUrlOrId);
            _settings.NamePrefix = EditorGUILayout.TextField("Имя начинается с",
                string.IsNullOrEmpty(_settings.NamePrefix)
                    ? "#"
                    : _settings.NamePrefix);

            if (GUILayout.Button("Загрузить папки"))
            {
                LoadSubfolders();
            }

            if (Folders.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Папки:", EditorStyles.boldLabel);
                var options = new string[Folders.Count];
                for (var i = 0; i < Folders.Count; i++)
                {
                    options[i] = $"{Folders[i].Name} ({Folders[i].Id})";
                }

                var selectedIndex = EditorGUILayout.Popup("Выбранная папка", SelectedFolderIndex, options);

                if (SelectedFolderIndex != selectedIndex)
                {
                    SelectedFolderIndex = selectedIndex;
                    Spreadsheets.Clear();
                    SelectedFiles.Clear();
                }

                if (SelectedFolderIndex >= 0 && GUILayout.Button("Загрузить таблицы из папки"))
                {
                    LoadSpreadsheets();
                }
            }

            if (Spreadsheets.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Google Sheets:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Выбрать все"))
                {
                    foreach (var f in Spreadsheets) SelectedFiles[f.Id] = true;
                }

                if (GUILayout.Button("Снять выделение"))
                {
                    foreach (var f in Spreadsheets) SelectedFiles[f.Id] = false;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField(
                    "Для открытия таблицы в браузере или проводнике используйте кнопки с конками",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Не рекомендуется импортировать всё сразу", EditorStyles.boldLabel);

                for (var index = 0; index < Spreadsheets.Count; index++)
                {
                    var file = Spreadsheets[index];
                    SelectedFiles.TryAdd(file.Id, false);

                    var label = $"{index}. {file.Name}";

                    EditorGUILayout.BeginHorizontal();
                    var rect = EditorGUILayout.GetControlRect();
                    SelectedFiles[file.Id] = EditorGUI.ToggleLeft(rect, label, SelectedFiles[file.Id]);

                    DrawParser(file.Name);
                    DrawOpenUrl(file.WebViewLink);
                    DrawOpenFile(file.Name);

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void DrawOpenFile(string fileName)
        {
            var linkIcon = EditorGUIUtility.IconContent("d_FolderOpened Icon");
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = lineHeight,
                fixedWidth = lineHeight * 2,
            };
            if (GUILayout.Button(linkIcon, style))
            {
                var fullPath = Path.Combine(_settings.OutputFolder, $"{fileName}.json");
                if (File.Exists(fullPath))
                {
                    EditorUtility.RevealInFinder(fullPath);
                }
                else
                {
                    ShowNotification(new GUIContent("Файл не найден"));
                }
            }
        }

        private void DrawOpenUrl(string fileWebViewLink)
        {
            var linkIcon = EditorGUIUtility.IconContent("d_Linked");
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = lineHeight,
                fixedWidth = lineHeight * 2,
                padding = new RectOffset(-4, -4, -4, -4)
            };
            if (GUILayout.Button(linkIcon, style))
            {
                Application.OpenURL(fileWebViewLink);
            }
        }

        private void DrawParser(string fileName)
        {
            GoogleSheetImporterSettings.Instance.TryGetParserProvider(fileName, out var parser);

            var newParser = (AbstractSheetParserProvider) EditorGUILayout.ObjectField(
                parser,
                typeof(AbstractSheetParserProvider),
                false
            );

            if (newParser != parser)
            {
                GoogleSheetImporterSettings.Instance.SetParserProvider(fileName, newParser);
            }
        }

        private void DrawOutput()
        {
            EditorGUILayout.LabelField("Вывод", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _settings.OutputFolder = EditorGUILayout.TextField("Папка для JSON", _settings.OutputFolder);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var selected = EditorUtility.OpenFolderPanel("Куда сохранить JSON", _settings.OutputFolder, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    var project = Directory.GetCurrentDirectory().Replace("\\", "/");
                    if (selected.Replace("\\", "/").StartsWith(project))
                    {
                        var output = selected.Replace("\\", "/").Substring(project.Length);
                        if (output.StartsWith("/"))
                        {
                            output = output.Substring(1);
                        }

                        _settings.OutputFolder = output;
                    }
                    else
                    {
                        _settings.OutputFolder = selected;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        public void ImportSelected()
        {
            var imported = 0;
            var selectedCount = SelectedFiles.Count(x => x.Value);

            try
            {
                _settings.EnsureOutputFolder();

                var sheetsSvc = _auth.CreateSheetsService();
                var sheets = new SheetsServiceWrapper(sheetsSvc);

                ShowProgress();
                foreach (var file in Spreadsheets)
                {
                    if (!SelectedFiles.TryGetValue(file.Id, out var isSelected) || !isSelected)
                    {
                        continue;
                    }

                    if (!GoogleSheetImporterSettings.Instance.TryGetParserProvider(file.Name, out var parserProvider))
                    {
                        Debug.LogError("Парсер не назначен для файла: " + file.Name);
                        continue;
                    }

                    var meta = sheets.GetSpreadsheetMeta(file.Id);
                    var sheetWrappers = new Dictionary<string, object>();
                    var wrapperObj = new Dictionary<string, object>();

                    foreach (var sheet in meta.Sheets)
                    {
                        var sheetTitle = sheet.Properties.Title;
                        if (!string.IsNullOrEmpty(_settings.NamePrefix) && !sheetTitle.StartsWith(_settings.NamePrefix))
                        {
                            continue;
                        }

                        var values = sheets.GetValues(file.Id, sheetTitle);

                        sheetTitle = sheetTitle.Replace(_settings.NamePrefix, string.Empty);

                        var parser = parserProvider.GetParser(sheetTitle);
                        var rows = parser.Parse(values);

                        sheetWrappers.TryAdd(sheetTitle, rows);
                    }

                    wrapperObj["DateTime"] = DateTime.UtcNow.ToString("d.MM.y h:mm:ss");
                    wrapperObj["ConfigName"] = meta.SpreadsheetName;
                    foreach (var wrapper in sheetWrappers)
                    {
                        wrapperObj.Add(wrapper.Key, wrapper.Value);
                    }

                    var fileName = $"{JsonExporter.SanitizeFileName(file.Name)}.json";
                    var path = Path.Combine(_settings.OutputFolder, fileName);
                    JsonExporter.Save(wrapperObj, path);
                    imported++;
                    ShowProgress();
                }

                AssetDatabase.Refresh();
                ShowNotification(new GUIContent($"Импортировано объектов: {imported}"));

                var fullPath = Path.GetFullPath(_settings.OutputFolder);

                Debug.Log($"[SheetsImporter] Импорт завершён. Файлы в: {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ShowNotification(new GUIContent("Ошибка импорта (см. Console)"));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return;

            void ShowProgress()
            {
                var current = imported;
                var total = selectedCount;

                if (total == 0)
                {
                    total = 1;
                }

                EditorUtility.DisplayProgressBar("Importing...",
                    $"Processing {current}/{total}",
                    (float) current / total);
            }
        }

        private void InitializeMappersList()
        {
            var mappers = GoogleSheetImporterSettings.Instance.Mappers;

            _mappersList = new ReorderableList(mappers,
                typeof(GoogleSheetImporterSettings.MappingConfig),
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Mappings"); },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = mappers[index];
                    var lineHeight = EditorGUIUtility.singleLineHeight;
                    var padding = 5f;

                    rect.y += 2;

                    var toggleWidth = 170f;
                    var buttonWidth = 200f;
                    var fieldWidth = (rect.width - toggleWidth - 3 * padding) / 2f - buttonWidth / 2f - padding;

                    var label = "Select mapper";

                    if (element.MapperProvider)
                    {
                        label = element.MapperProvider.GetDisplayName();
                    }

                    element.Selected = EditorGUI.ToggleLeft(
                        new Rect(rect.x, rect.y, toggleWidth, lineHeight),
                        label,
                        element.Selected
                    );

                    if (GUI.Button(
                            new Rect(rect.x + toggleWidth + padding, rect.y, buttonWidth, lineHeight),
                            "Открыть конфиг"))
                    {
                        var target = element.MapperProvider.GetTarget();

                        if (target)
                        {
                            EditorGUIUtility.PingObject(target);
                            Selection.activeObject = target;
                        }
                    }


                    element.MapperProvider = (AbstractConfigMapperProvider) EditorGUI.ObjectField(
                        new Rect(rect.x + toggleWidth + padding + fieldWidth + padding,
                            rect.y,
                            fieldWidth,
                            lineHeight),
                        element.MapperProvider,
                        typeof(AbstractConfigMapperProvider),
                        false
                    );

                    if (GUI.Button(
                            new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, lineHeight),
                            "Применить текущий JSON"))
                    {
                        ApplyMapping(element);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                },
                onAddCallback = list =>
                {
                    mappers.Add(new GoogleSheetImporterSettings.MappingConfig());
                    EditorUtility.SetDirty(GoogleSheetImporterSettings.Instance);
                },
                onRemoveCallback = list =>
                {
                    mappers.RemoveAt(list.index);
                    EditorUtility.SetDirty(GoogleSheetImporterSettings.Instance);
                }
            };
        }

        private void DrawApply()
        {
            _mappersList.DoLayoutList();
            var selected = GoogleSheetImporterSettings.Instance.Mappers.Where(x => x.Selected).ToList();

            using (new EditorGUI.DisabledScope(_mappersList.count == 0 || selected.Count == 0))
            {
                if (GUILayout.Button("Обновить выбранные из текущих JSON"))
                {
                    foreach (var mapping in selected)
                    {
                        ApplyMapping(mapping);
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }

        private void ApplyMapping(GoogleSheetImporterSettings.MappingConfig mapping)
        {
            var provider = mapping.MapperProvider;
            provider.GetMapper().Apply();
            provider.HandleConfigUpdated();

            var target = provider.GetTarget();

            EditorUtility.SetDirty(target);
        }
    }
}

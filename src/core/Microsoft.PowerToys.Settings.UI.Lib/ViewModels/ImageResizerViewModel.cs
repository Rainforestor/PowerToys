﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.PowerToys.Settings.UI.Lib.Helpers;
using Microsoft.PowerToys.Settings.UI.Lib.Interface;

namespace Microsoft.PowerToys.Settings.UI.Lib.ViewModels
{
    public class ImageResizerViewModel : Observable
    {
        private GeneralSettings GeneralSettingsConfig { get; set; }

        private readonly ISettingsUtils _settingsUtils;

        private ImageResizerSettings Settings { get; set; }

        private const string ModuleName = ImageResizerSettings.ModuleName;

        private Func<string, int> SendConfigMSG { get; }

        public ImageResizerViewModel(ISettingsUtils settingsUtils, ISettingsRepository<GeneralSettings> settingsRepository, Func<string, int> ipcMSGCallBackFunc)
        {
            _settingsUtils = settingsUtils ?? throw new ArgumentNullException(nameof(settingsUtils));

            // To obtain the general settings configurations of PowerToys.
            GeneralSettingsConfig = settingsRepository.SettingsConfig;

            try
            {
                Settings = _settingsUtils.GetSettings<ImageResizerSettings>(ModuleName);
            }
            catch
            {
                Settings = new ImageResizerSettings();
                _settingsUtils.SaveSettings(Settings.ToJsonString(), ModuleName);
            }

            // set the callback functions value to hangle outgoing IPC message.
            SendConfigMSG = ipcMSGCallBackFunc;

            _isEnabled = GeneralSettingsConfig.Enabled.ImageResizer;
            _advancedSizes = Settings.Properties.ImageresizerSizes.Value;
            _jpegQualityLevel = Settings.Properties.ImageresizerJpegQualityLevel.Value;
            _pngInterlaceOption = Settings.Properties.ImageresizerPngInterlaceOption.Value;
            _tiffCompressOption = Settings.Properties.ImageresizerTiffCompressOption.Value;
            _fileName = Settings.Properties.ImageresizerFileName.Value;
            _keepDateModified = Settings.Properties.ImageresizerKeepDateModified.Value;
            _encoderGuidId = GetEncoderIndex(Settings.Properties.ImageresizerFallbackEncoder.Value);

            int i = 0;
            foreach (ImageSize size in _advancedSizes)
            {
                size.Id = i;
                i++;
                size.PropertyChanged += Size_PropertyChanged;
            }
        }

        private bool _isEnabled = false;
        private ObservableCollection<ImageSize> _advancedSizes = new ObservableCollection<ImageSize>();
        private int _jpegQualityLevel = 0;
        private int _pngInterlaceOption;
        private int _tiffCompressOption;
        private string _fileName;
        private bool _keepDateModified;
        private int _encoderGuidId = 0;

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }

            set
            {
                if (value != _isEnabled)
                {
                    // To set the status of ImageResizer in the General PowerToys settings.
                    _isEnabled = value;
                    GeneralSettingsConfig.Enabled.ImageResizer = value;
                    OutGoingGeneralSettings snd = new OutGoingGeneralSettings(GeneralSettingsConfig);

                    SendConfigMSG(snd.ToString());
                    OnPropertyChanged("IsEnabled");
                }
            }
        }

        public ObservableCollection<ImageSize> Sizes
        {
            get
            {
                return _advancedSizes;
            }

            set
            {
                SavesImageSizes(value);
                _advancedSizes = value;
                OnPropertyChanged("Sizes");
            }
        }

        public int JPEGQualityLevel
        {
            get
            {
                return _jpegQualityLevel;
            }

            set
            {
                if (_jpegQualityLevel != value)
                {
                    _jpegQualityLevel = value;
                    Settings.Properties.ImageresizerJpegQualityLevel.Value = value;
                    _settingsUtils.SaveSettings(Settings.ToJsonString(), ModuleName);
                    OnPropertyChanged("JPEGQualityLevel");
                }
            }
        }

        public int PngInterlaceOption
        {
            get
            {
                return _pngInterlaceOption;
            }

            set
            {
                if (_pngInterlaceOption != value)
                {
                    _pngInterlaceOption = value;
                    Settings.Properties.ImageresizerPngInterlaceOption.Value = value;
                    _settingsUtils.SaveSettings(Settings.ToJsonString(), ModuleName);
                    OnPropertyChanged("PngInterlaceOption");
                }
            }
        }

        public int TiffCompressOption
        {
            get
            {
                return _tiffCompressOption;
            }

            set
            {
                if (_tiffCompressOption != value)
                {
                    _tiffCompressOption = value;
                    Settings.Properties.ImageresizerTiffCompressOption.Value = value;
                    _settingsUtils.SaveSettings(Settings.ToJsonString(), ModuleName);
                    OnPropertyChanged("TiffCompressOption");
                }
            }
        }

        public string FileName
        {
            get
            {
                return _fileName;
            }

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _fileName = value;
                    Settings.Properties.ImageresizerFileName.Value = value;
                    _settingsUtils.SaveSettings(Settings.ToJsonString(), ModuleName);
                    OnPropertyChanged("FileName");
                }
            }
        }

        public bool KeepDateModified
        {
            get
            {
                return _keepDateModified;
            }

            set
            {
                _keepDateModified = value;
                Settings.Properties.ImageresizerKeepDateModified.Value = value;
                _settingsUtils.SaveSettings(Settings.ToJsonString(), ModuleName);
                OnPropertyChanged("KeepDateModified");
            }
        }

        public int Encoder
        {
            get
            {
                return _encoderGuidId;
            }

            set
            {
                if (_encoderGuidId != value)
                {
                    _encoderGuidId = value;
                    _settingsUtils.SaveSettings(Settings.Properties.ImageresizerSizes.ToJsonString(), ModuleName, "sizes.json");
                    Settings.Properties.ImageresizerFallbackEncoder.Value = GetEncoderGuid(value);
                    _settingsUtils.SaveSettings(Settings.ToJsonString(), ModuleName);
                    OnPropertyChanged("Encoder");
                }
            }
        }

        public void AddRow()
        {
            ObservableCollection<ImageSize> imageSizes = Sizes;
            int maxId = imageSizes.Count > 0 ? imageSizes.OrderBy(x => x.Id).Last().Id : -1;
            ImageSize newSize = new ImageSize(maxId + 1);
            newSize.PropertyChanged += Size_PropertyChanged;
            imageSizes.Add(newSize);
            _advancedSizes = imageSizes;
            SavesImageSizes(imageSizes);
        }

        public void DeleteImageSize(int id)
        {
            ImageSize size = _advancedSizes.Where<ImageSize>(x => x.Id == id).First();
            ObservableCollection<ImageSize> imageSizes = Sizes;
            imageSizes.Remove(size);

            _advancedSizes = imageSizes;
            SavesImageSizes(imageSizes);
        }

        public void SavesImageSizes(ObservableCollection<ImageSize> imageSizes)
        {
            _settingsUtils.SaveSettings(Settings.Properties.ImageresizerSizes.ToJsonString(), ModuleName, "sizes.json");
            Settings.Properties.ImageresizerSizes = new ImageResizerSizes(imageSizes);
            _settingsUtils.SaveSettings(Settings.ToJsonString(), ModuleName);
        }

        public string GetEncoderGuid(int value)
        {
            // PNG Encoder guid
            if (value == 0)
            {
                return "1b7cfaf4-713f-473c-bbcd-6137425faeaf";
            }

            // Bitmap Encoder guid
            else if (value == 1)
            {
                return "0af1d87e-fcfe-4188-bdeb-a7906471cbe3";
            }

            // JPEG Encoder guid
            else if (value == 2)
            {
                return "19e4a5aa-5662-4fc5-a0c0-1758028e1057";
            }

            // Tiff encoder guid.
            else if (value == 3)
            {
                return "163bcc30-e2e9-4f0b-961d-a3e9fdb788a3";
            }

            // Tiff encoder guid.
            else if (value == 4)
            {
                return "57a37caa-367a-4540-916b-f183c5093a4b";
            }

            // Gif encoder guid.
            else if (value == 5)
            {
                return "1f8a5601-7d4d-4cbd-9c82-1bc8d4eeb9a5";
            }

            return null;
        }

        public int GetEncoderIndex(string guid)
        {
            // PNG Encoder guid
            if (guid == "1b7cfaf4-713f-473c-bbcd-6137425faeaf")
            {
                return 0;
            }

            // Bitmap Encoder guid
            else if (guid == "0af1d87e-fcfe-4188-bdeb-a7906471cbe3")
            {
                return 1;
            }

            // JPEG Encoder guid
            else if (guid == "19e4a5aa-5662-4fc5-a0c0-1758028e1057")
            {
                return 2;
            }

            // Tiff encoder guid.
            else if (guid == "163bcc30-e2e9-4f0b-961d-a3e9fdb788a3")
            {
                return 3;
            }

            // Tiff encoder guid.
            else if (guid == "57a37caa-367a-4540-916b-f183c5093a4b")
            {
                return 4;
            }

            // Gif encoder guid.
            else if (guid == "1f8a5601-7d4d-4cbd-9c82-1bc8d4eeb9a5")
            {
                return 5;
            }

            return -1;
        }

        public void Size_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ImageSize modifiedSize = (ImageSize)sender;
            ObservableCollection<ImageSize> imageSizes = Sizes;
            imageSizes.Where<ImageSize>(x => x.Id == modifiedSize.Id).First().Update(modifiedSize);
            _advancedSizes = imageSizes;
            SavesImageSizes(imageSizes);
        }
    }
}

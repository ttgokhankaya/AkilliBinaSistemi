using DataAccess;
using SimulationDB_Migrations;
using SimulationObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GUI_Simulation
{
    public partial class MainWindowForRules : Window, INotifyPropertyChanged
    {
        public MainWindowForRules()
        {
            InitializeComponent();
            DataContext = this;

            CollapsAll();
            LoadAsync().ConfigureAwait(false);
        }

        private async Task LoadAsync()
        {
            plnContainer.IsEnabled = false;
            lblMesaj.Text = "Veriler yüklenirken lütfen bekleyiniz.";

            await LoadAreasAsync();

            await LoadDevicesAsync();

            await LoadOperationsAsync();

            plnContainer.IsEnabled = true;
            lblMesaj.Text = "Lütfen yapmak istediğiniz işlemi seçiniz.";




            /*
            loadListBox();
            loadListBoxScenario();
            loadListBoxActor();
            loadListBoxHabit();
            */
        }


        Actor actor = new Actor();
        Scenario scenario = new Scenario();
        Habit habit = new Habit();
        Operation operation = new Operation();
        DeviceBase deviceBase = new DeviceBase();
        AreaBase areaBase = new AreaBase();
        ArrayList aliskanlikList;
        ArrayList aliskanlikListSag;
        string currentItem = string.Empty;
        int index = 0;


        #region Fileds
        private List<AreaBase> _areas;
        private List<DeviceBase> _devices;
        private List<Operation> _operations;



        private string _deviceName;
        private string _ipName;

        public string _operationName;
        public string _operationTime;
        public string _operationSpan;

        private List<DeviceBase> _islemeEklenebilirCihazlar;
        private List<AreaBase> _islemeEklenmisAlanlar;
        private Operation _islemIcinkayitEdilecekOperasyon;



        #endregion Fileds

        #region Properties
        public List<AreaBase> Areas
        {
            get
            {
                if (_areas == null)
                {
                    _areas = new List<AreaBase>();
                }
                return _areas;
            }

            set
            {
                _areas = value;
                OnPropertyChanged();
            }
        }

        public List<DeviceBase> Devices
        {
            get
            {
                if (_devices == null)
                {
                    _devices = new List<DeviceBase>();
                }
                return _devices;
            }

            set
            {
                _devices = value;
                OnPropertyChanged();
            }
        }

        public List<Operation> Operations
        {
            get
            {
                return _operations;
            }

            set
            {
                _operations = value;
                OnPropertyChanged();
            }
        }


        public string deviceName
        {
            get { return _deviceName; }
            set
            {
                if (value != _deviceName)
                {
                    _deviceName = value;
                    OnPropertyChanged();
                }
            }

        }
        public string ipName
        {
            get { return _ipName; }
            set
            {
                if (value != _ipName)
                {
                    _ipName = value;
                    OnPropertyChanged();
                }
            }
        }
        public string OperationName
        {
            get { return _operationName; }
            set
            {
                if (value != _operationName)
                {
                    _operationName = value;
                    OnPropertyChanged();
                }
            }
        }
        public string OperationSpan
        {
            get
            {
                if (_operations == null)
                    _operations = new List<Operation>();

                return _operationSpan;
            }
            set
            {
                if (value != _operationSpan)
                {
                    _operationSpan = value;
                    OnPropertyChanged();
                }
            }
        }
        public string OperationTime
        {
            get { return _operationTime; }
            set
            {
                if (value != _operationTime)
                {
                    _operationTime = value;
                    OnPropertyChanged();
                }
            }
        }


        #endregion Properties

        #region INotifyPropertyChanged Implementaion

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged Implementaion

        #region Navigation

        #region Navigation Properties

        #region Alanlar
        private Visibility _alanListesiGoster;

        [Shower]
        [ListShower]
        public Visibility AlanListesiGoster
        {
            get
            {
                return _alanListesiGoster;
            }

            set
            {
                _alanListesiGoster = value;
                OnPropertyChanged();
            }
        }


        private Visibility _alanCrudGoster;

        [Shower]
        [CrudShower]
        public Visibility AlanCrudGoster
        {
            get
            {
                return _alanCrudGoster;
            }

            set
            {
                _alanCrudGoster = value;
                OnPropertyChanged();
            }
        }

        #endregion Alanlar  

        #region Cihazlar
        private Visibility _cihazListesiGoster;

        [Shower]
        [ListShower]
        public Visibility CihazListesiGoster
        {
            get
            {
                return _cihazListesiGoster;
            }

            set
            {
                _cihazListesiGoster = value;
                OnPropertyChanged();
            }
        }


        private Visibility _cihazCrudGoster;

        [Shower]
        [CrudShower]
        public Visibility CihazCrudGoster
        {
            get
            {
                return _cihazCrudGoster;
            }

            set
            {
                _cihazCrudGoster = value;
                OnPropertyChanged();
            }
        }


        #endregion Cihazlar  

        #region İşlemler
        private Visibility _islemListesiGoster;

        [Shower]
        [ListShower]
        public Visibility IslemListesiGoster
        {
            get
            {
                return _islemListesiGoster;
            }

            set
            {
                _islemListesiGoster = value;
                OnPropertyChanged();
            }
        }


        private Visibility _islemCrudGoster;

        [Shower]
        [CrudShower]
        public Visibility IslemCrudGoster
        {
            get
            {
                return _islemCrudGoster;
            }

            set
            {
                _islemCrudGoster = value;
                OnPropertyChanged();
            }
        }



        #endregion Alanlar  

        #region Alışkanlıklar
        private Visibility _aliskanlikListesiGoster;

        [Shower]
        [ListShower]
        public Visibility AliskanlikListesiGoster
        {
            get
            {
                return _aliskanlikListesiGoster;
            }

            set
            {
                _aliskanlikListesiGoster = value;
                OnPropertyChanged();
            }
        }


        private Visibility _aliskanlıkCrudGoster;

        [Shower]
        [CrudShower]
        public Visibility AliskanlikCrudGoster
        {
            get
            {
                return _aliskanlıkCrudGoster;
            }

            set
            {
                _aliskanlıkCrudGoster = value;
                OnPropertyChanged();
            }
        }

        public string _habitName;

        public string habitName
        {
            get { return _habitName; }
            set
            {
                if (value != _habitName)
                {
                    _habitName = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion Alanlar  

        #region Kişiler
        private Visibility _kisiListesiGoster;

        [Shower]
        [ListShower]
        public Visibility KisiListesiGoster
        {
            get
            {
                return _kisiListesiGoster;
            }

            set
            {
                _kisiListesiGoster = value;
                OnPropertyChanged();
            }
        }


        private Visibility _kisiCrudGoster;
        public string _areaName;

        public string AreaName
        {
            get { return _areaName; }
            set
            {
                if (value != _areaName)
                {
                    _areaName = value;
                    OnPropertyChanged();
                }
            }
        }

        [Shower]
        [CrudShower]
        public Visibility KisiCrudGoster
        {
            get
            {
                return _kisiCrudGoster;
            }

            set
            {
                _kisiCrudGoster = value;
                OnPropertyChanged();
            }
        }
        public string _actorName;

        public string actorName
        {
            get { return _actorName; }
            set
            {
                if (value != _actorName)
                {
                    _actorName = value;
                    OnPropertyChanged();
                }
            }

        }
        #endregion Alanlar

        #region Senaryolar
        private Visibility _senaryoListesiGoster;

        [Shower]
        [ListShower]
        public Visibility SenaryoListesiGoster
        {
            get
            {
                return _senaryoListesiGoster;
            }

            set
            {
                _senaryoListesiGoster = value;
                OnPropertyChanged();
            }
        }


        private Visibility _senaryoCrudGoster;
        public string _scenarioName;
        public String scenarioName
        {
            get { return _scenarioName; }
            set
            {
                if (value != _scenarioName)
                {
                    _scenarioName = value;
                    OnPropertyChanged();
                }
            }
        }

        [Shower]
        [CrudShower]
        public Visibility SenaryoCrudGoster
        {
            get
            {
                return _senaryoCrudGoster;
            }

            set
            {
                _senaryoCrudGoster = value;
                OnPropertyChanged();
            }
        }


        #endregion Alanlar  

        #endregion Navigation Properties


        private void BtnGoster_Click(object sender, RoutedEventArgs e)
        {
            CollapsAll();

            ShowSender(sender);
        }

        private void CollapsAll()
        {
            var properties = GetType().GetProperties().Where(x => x.GetCustomAttributes(typeof(ShowerAttribute), false).Length > 0).ToList();

            foreach (var prop in properties)
            {
                prop.SetValue(this, Visibility.Collapsed);
            }
        }

        private void ShowSender(object sender)
        {
            var property = GetType().GetProperties().Where(x => x.GetCustomAttributes(typeof(ShowerAttribute), false).Length > 0 && x.Name == ((Button)sender).Tag.ToString()).FirstOrDefault();

            property?.SetValue(this, Visibility.Visible);
        }

        #endregion Navigation

        #region Private Methods

        #region Genel
        private void LoadOnListbox(ref ListBox control, ICollection list)
        {
            control.Items.Clear();
            if (list.Count <= 0)
            {
                control.Items.Add("Eklenmiş öğe bulunamadı");
            }

            foreach (var item in list)
            {
                ListBoxItem itm = new ListBoxItem();
                itm.Content = ((IShowableOnListcs)item).GetValue();
                itm.Tag = item;
                control.Items.Add(itm);
            }
        }

        private DataAccess.Repository.IUnitOfWork GetOuw()
        {
            return UnitOfWorkFactory.CreateBasicContext(new DB());
        }

        private bool Kaydet(DataAccess.Repository.IUnitOfWork uow)
        {
            if (uow == null)
                return false;

            bool result = uow.SaveChanges() > 0;
            if (result)
                MessageBox.Show("İşlem Başarılı");

            return result;
        }

        private void comboxDoldur(ref ComboBox control, ICollection list)
        {
            control.Items.Clear();
            foreach (var item in list)
            {
                ComboBoxItem cmbItm = new ComboBoxItem();
                cmbItm.Content = ((IShowableOnListcs)item).GetValue();
                cmbItm.Tag = item;
                control.Items.Add(cmbItm);
            }

            control.SelectedIndex = 0;
        }


        #endregion Genel

        #region Area Methods
        private async Task LoadAreasAsync()
        {
            await Task.Run(() =>
            {
                GetAreaList();
            });

            LoadOnListbox(ref AreaListbox, Areas);
            comboxDoldur(ref cmbAreaOfDevice, Areas);
            LoadOnListbox(ref lsbIsleminAlanlariHepsi, Areas);
        }

        private void GetAreaList()
        {
            using (var ouw = GetOuw())
            {
                var repo = ouw.Repository<AreaBase>();
                var data = repo.FindAll().Include("DevicesInArea");
                if (data == null)
                    return;

                Areas = data.ToList();
            }

        }

        private void BtnKaydet_ClickArea(object sender, RoutedEventArgs e)
        {
            AddToAreas();
        }

        private void AddToAreas()
        {
            if (string.IsNullOrEmpty(AreaName))
                return;

            using (var uow = GetOuw())
            {
                var repo = uow.Repository<AreaBase>();
                var data = repo.Add(new AreaBase() { Name = AreaName });

                if (Kaydet(uow))
                {
                    AreaName = "";
                    LoadAreasAsync().ConfigureAwait(false);
                }

            }
        }

        #endregion Area Methods

        #region Device Methods

        private void GetAndLoadDeviceTypes()
        {
            var asm = Assembly.GetAssembly(typeof(DeviceBase));
            var types = asm.GetTypes().Where(x => x.GetCustomAttributes(typeof(DeviceDefitionAttribute), false).Length > 0).ToList();

            cmbDevicesTypes_Doldur(types);

            comboxDoldur(ref cmbAreaOfDevice, Areas);
        }

        private void cmbDevicesTypes_Doldur(List<Type> types)
        {
            cmbDeviceTypes.Items.Clear();
            foreach (var item in types)
            {
                ComboBoxItem cmbItm = new ComboBoxItem();
                cmbItm.Content = item.Name;
                cmbItm.Tag = item;
                cmbDeviceTypes.Items.Add(cmbItm);
            }

            cmbDeviceTypes.SelectedIndex = 0;
        }

        private void BtnKaydet_ClickDevice(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(deviceName))
                return;

            if (string.IsNullOrEmpty(ipName))
                return;

            if (cmbDeviceTypes.SelectedItem == null)
                return;

            if (cmbAreaOfDevice.SelectedItem == null)
                return;

            var device = Activator.CreateInstance((Type)((ComboBoxItem)cmbDeviceTypes.SelectedItem).Tag);
            ((DeviceBase)device).Name = deviceName;
            ((DeviceBase)device).ip = ipName;
            ((DeviceBase)device).AreaID = ((AreaBase)((ComboBoxItem)cmbAreaOfDevice.SelectedItem).Tag).ID;

            using (var uow = GetOuw())
            {
                var repo = uow.Repository<DeviceBase>();
                var data = repo.Add((DeviceBase)device);

                if (Kaydet(uow))
                {
                    LoadDevicesAsync().ConfigureAwait(false);

                    cmbDeviceTypes.SelectedIndex = 0;
                    cmbDeviceTypes.SelectedIndex = 0;
                    deviceName = "";
                    ipName = "";
                }
            }
        }

        private async Task LoadDevicesAsync()
        {
            await Task.Run(() =>
            {
                loadListBoxDevice();
            });

            LoadOnListbox(ref DeviceListBox, Devices);
            GetAndLoadDeviceTypes();
        }


        private void loadListBoxDevice()
        {
            using (var ouw = GetOuw())
            {
                var repo = ouw.Repository<DeviceBase>();
                var data = repo.FindAll().Include("AreaBase");
                if (data == null)
                    return;
                Devices = data.ToList();
            }
        }

        #endregion Device Methods

        #region Operations

        private async Task LoadOperationsAsync()
        {
            await Task.Run(() =>
            {
                using (var ouw = GetOuw())
                {
                    var repo = ouw.Repository<Operation>();
                    var data = repo.FindAll().Include("DevicesOfOperation").Include("DevicesOfOperation.DeviceBase").Include("DevicesOfOperation.DeviceBase.AreaBase");
                    if (data == null)
                        return;
                    Operations = data.ToList();
                }
            });

            _islemIcinkayitEdilecekOperasyon = new Operation();
            _islemIcinkayitEdilecekOperasyon.DevicesOfOperation = new List<OperationDevice>();

            LoadOnListbox(ref IslemlerListBox, Operations);

            lsbIsleminAlanlariSecilenler.Items.Clear();
            lsbIsleminCihazlariSecilenler.Items.Clear();

            _islemeEklenebilirCihazlar = new List<DeviceBase>();
            _islemeEklenmisAlanlar = new List<AreaBase>();
        }


        private void btnAddAreasToOperation_Click(object sender, RoutedEventArgs e)
        {
            if (lsbIsleminAlanlariHepsi.SelectedItem == null)
            {
                return;
            }

            var seciliAlan = (AreaBase)((ListBoxItem)lsbIsleminAlanlariHepsi.SelectedItem).Tag;


            lsbIsleminAlanlariHepsi.Items.RemoveAt(lsbIsleminAlanlariHepsi.SelectedIndex);
            lsbIsleminAlanlariSecilenler.Items.Add(new ListBoxItem() { Tag = seciliAlan, Content = seciliAlan.Name });

            _islemeEklenmisAlanlar.Add(seciliAlan);

            foreach (var deviceInArea in seciliAlan.DevicesInArea)
            {
                if (_islemeEklenebilirCihazlar.Exists(x => x.ID == deviceInArea.ID))
                    continue;

                _islemeEklenebilirCihazlar.Add(deviceInArea);

                lsbIsleminCihazlariHepsi.Items.Add(new ListBoxItem() { Tag = deviceInArea, Content = deviceInArea.Type + " " + deviceInArea.ip });
            }
        }

        private void btnRemoveAreasfromOperation_Click(object sender, RoutedEventArgs e)
        {
            if (lsbIsleminAlanlariSecilenler.SelectedItem == null)
            {
                return;
            }

            var seciliAlan = (AreaBase)((ListBoxItem)lsbIsleminAlanlariSecilenler.SelectedItem).Tag;


            lsbIsleminAlanlariSecilenler.Items.RemoveAt(lsbIsleminAlanlariSecilenler.SelectedIndex);
            lsbIsleminAlanlariHepsi.Items.Add(new ListBoxItem() { Tag = seciliAlan, Content = seciliAlan.Name });
            _islemeEklenmisAlanlar.Remove(_islemeEklenmisAlanlar.Find(x => x.ID == seciliAlan.ID));

            foreach (var deviceInArea in seciliAlan.DevicesInArea)
            {
                _islemeEklenebilirCihazlar.Remove(_islemeEklenebilirCihazlar.Find(x => x.ID == deviceInArea.ID));

                for (int i = 0; i < lsbIsleminCihazlariSecilenler.Items.Count; i++)
                {
                    var device = (DeviceBase)((ListBoxItem)lsbIsleminCihazlariSecilenler.Items[i]).Tag;

                    if (deviceInArea.ID == device.ID)
                    {
                        lsbIsleminCihazlariSecilenler.Items.RemoveAt(i);
                        i--;
                        continue;
                    }
                }

                for (int i = 0; i < lsbIsleminCihazlariHepsi.Items.Count; i++)
                {
                    var device = (DeviceBase)((ListBoxItem)lsbIsleminCihazlariHepsi.Items[i]).Tag;

                    if (deviceInArea.ID == device.ID)
                    {
                        lsbIsleminCihazlariHepsi.Items.RemoveAt(i);
                        i--;
                        continue;
                    }
                }

                for (int i = 0; i < _islemIcinkayitEdilecekOperasyon.DevicesOfOperation.Count; i++)
                {
                    if (_islemIcinkayitEdilecekOperasyon.DevicesOfOperation[i].DeviceBaseID == deviceInArea.ID)
                    {
                        _islemIcinkayitEdilecekOperasyon.DevicesOfOperation.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
            }
        }

        private void btnAddDevicesToOperation_Click(object sender, RoutedEventArgs e)
        {
            if (lsbIsleminCihazlariHepsi.SelectedItem == null)
            {
                return;
            }

            var seciliCihaz = (DeviceBase)((ListBoxItem)lsbIsleminCihazlariHepsi.SelectedItem).Tag;

            _islemeEklenebilirCihazlar.Add(seciliCihaz);
            lsbIsleminCihazlariSecilenler.Items.Add(new ListBoxItem() { Tag = seciliCihaz, Content = seciliCihaz.Type + " " + seciliCihaz.ip });
        }

        private void btnRemoveDevicesfromOperation_Click(object sender, RoutedEventArgs e)
        {
            if (lsbIsleminCihazlariSecilenler.SelectedItem == null)
            {
                return;
            }

            var seciliCihaz = (DeviceBase)((ListBoxItem)lsbIsleminCihazlariSecilenler.SelectedItem).Tag;
            lsbIsleminCihazlariSecilenler.Items.RemoveAt(lsbIsleminCihazlariSecilenler.SelectedIndex);
            _islemeEklenebilirCihazlar.Remove(_islemeEklenebilirCihazlar.Find(x => x.ID == seciliCihaz.ID));

            for (int i = 0; i < _islemIcinkayitEdilecekOperasyon.DevicesOfOperation.Count; i++)
            {
                if (_islemIcinkayitEdilecekOperasyon.DevicesOfOperation[i].DeviceBaseID == seciliCihaz.ID)
                {
                    _islemIcinkayitEdilecekOperasyon.DevicesOfOperation.RemoveAt(i);
                    i--;
                    continue;
                }
            }
        }

        private void lsbIsleminCihazlariSecilenler_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lsbIsleminCihazlariSecilenler.SelectedItem == null)
            {
                lsbIsleminCihazlarininAksiyonlari.Items.Clear();
                return;
            }

            var seciliCihaz = (DeviceBase)((ListBoxItem)lsbIsleminCihazlariSecilenler.SelectedItem).Tag;
            var list = seciliCihaz.GetActionableMethodsOFDevice();
            lsbIsleminCihazlarininAksiyonlari.Items.Clear();
            foreach (var minfo in list)
            {
                ListBoxItem item = new ListBoxItem()
                {
                    Content = minfo.Name,
                    Tag = minfo
                };

                lsbIsleminCihazlarininAksiyonlari.Items.Add(item);
            }
        }

        private void btnOpereationEkle_Click(object sender, RoutedEventArgs e)
        {
            if (lsbIsleminCihazlarininAksiyonlari.SelectedItem == null)
            {
                return;
            }

            if (lsbIsleminCihazlariSecilenler.SelectedItem == null)
            {
                lsbIsleminCihazlarininAksiyonlari.Items.Clear();
                return;
            }
            int hour = 0;
            int min = 0;
            try
            {
                hour = int.Parse(OperationTime.Split(':')[0]);
                min = int.Parse(OperationTime.Split(':')[1]);

                _islemIcinkayitEdilecekOperasyon.Name = OperationName;
                _islemIcinkayitEdilecekOperasyon.StartTime = new DateTime(2000, 1, 1, hour, min, 0);
                _islemIcinkayitEdilecekOperasyon.Duration = new TimeSpan(0, int.Parse(OperationSpan), 0);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Saat formatının doğruluğundan emin olunuz - " + ex.Message);

                return;
            }

            var seciliCihaz = (DeviceBase)((ListBoxItem)lsbIsleminCihazlariSecilenler.SelectedItem).Tag;

            _islemIcinkayitEdilecekOperasyon.DevicesOfOperation.Add(new OperationDevice()
            {
                ActionName = lsbIsleminCihazlarininAksiyonlari.SelectedItem.ToString(),
                AreaID = seciliCihaz.AreaID,
                DeviceBaseID = seciliCihaz.ID,
                Sira = _islemIcinkayitEdilecekOperasyon.DevicesOfOperation.Count + 1
            });

            btnOperasonuDenetle_Click(null, null);
        }


        private void btnOperasonuDenetle_Click(object sender, RoutedEventArgs e)
        {
            string detay = "";

            detay += "Operasyon adı:" + _islemIcinkayitEdilecekOperasyon.Name + "\n";
            detay += "başlangıç Saati:" + _islemIcinkayitEdilecekOperasyon.StartTime.ToShortTimeString() + "\n";
            detay += _islemIcinkayitEdilecekOperasyon.StartTime.ToShortDateString() + " --> " + _islemIcinkayitEdilecekOperasyon.StartTime.AddMinutes(_islemIcinkayitEdilecekOperasyon.Duration.Minutes).ToShortTimeString() + "\n";

            foreach (var item in _islemIcinkayitEdilecekOperasyon.DevicesOfOperation)
            {
                detay += Areas.Find(x => x.ID == item.AreaID).Name + "\n";

                detay += Devices.Find(x => x.ID == item.DeviceBaseID).ToString() + "\n";

                detay += item.ActionName + "\n";
            }

            MessageBox.Show(detay);
        }

        private void btnOpereationKaydet_Click(object sender, RoutedEventArgs e)
        {
            int hour = int.Parse(OperationTime.Split(':')[0]);
            int min = int.Parse(OperationTime.Split(':')[1]);

            _islemIcinkayitEdilecekOperasyon.Name = OperationName;
            _islemIcinkayitEdilecekOperasyon.StartTime = new DateTime(1999, 1, 1, hour, min, 0);
            _islemIcinkayitEdilecekOperasyon.Duration = new TimeSpan(0, int.Parse(OperationSpan), 0);

            using (var uow = GetOuw())
            {
                uow.Repository<Operation>().Add(_islemIcinkayitEdilecekOperasyon);

                Kaydet(uow);
            }

        }

        #endregion

        private void BtnKaydet_ClickActor(object sender, RoutedEventArgs e)
        {
            using (var uow = GetOuw())
            {
                var repo = uow.Repository<Actor>();
                var data = repo.Add(new Actor() { Name = actorName, HabitsOfActor = null });

                Kaydet(uow);
            }

        }

        private void BtnKaydet_ClickScenario(object sender, RoutedEventArgs e)
        {
            using (var uow = GetOuw())
            {
                var repo = uow.Repository<Scenario>();
                var data = repo.Add(new Scenario() { Name = scenarioName, ActorsInScenario = null });

                Kaydet(uow);
            }
        }

        private void BtnKaydet_ClickHabit(object sender, RoutedEventArgs e)
        {
            using (var uow = GetOuw())
            {
                var repo = uow.Repository<Habit>();
                var data = repo.Add(new Habit() { Name = habitName });

                Kaydet(uow);
            }
        }

        private void BtnKaydet_ClickOperation(object sender, RoutedEventArgs e)
        {
            using (var uow = GetOuw())
            {
                var repo = uow.Repository<Operation>();
                var data = repo.Add(new Operation() { Name = OperationName });

                Kaydet(uow);
            }
        }


        private void BtnClick_SagaAktar(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedValue == null)
            {
                return;
            }

            currentItem = listBox.SelectedValue.ToString();
            index = listBox.SelectedIndex;

            ListBoxItem itm = new ListBoxItem();
            itm.Content = index;
            itm.Tag = index;
            listBox1.Items.Add(itm);

            //listBox1.Items.Add(currentItem);
            if (aliskanlikList != null)
            {
                aliskanlikList.RemoveAt(index);
            }
            loadAliskanlikListSag();
        }

        private void loadAliskanlikListSag()
        {
            listBox.ItemsSource = null;
            listBox.ItemsSource = aliskanlikList;

        }

        private void BtnClick_SolaAktar(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedValue == null)
            {
                return;
            }

            currentItem = listBox.SelectedValue.ToString();
            if (listBox1.SelectedIndex == 0)
            {
                index = listBox1.SelectedIndex;
                aliskanlikList.Add(currentItem);
                listBox1.Items.RemoveAt(listBox1.Items.IndexOf(listBox1.SelectedItem));
                loadAliskanlikListSag();
            }
            else
            {
                MessageBox.Show("Lütfen listeden kayıt seçiniz.");
                return;
            }

        }


        private void loadListBox()
        {
            listBox.Items.Clear();
            List<Habit> list = new List<Habit>();
            using (var ouw = GetOuw())
            {
                var repo = ouw.Repository<Habit>();
                var data = repo.FindAll();
                list = data.ToList();
            }
            foreach (var item in list)
            {
                ListBoxItem itm = new ListBoxItem();
                itm.Content = item.Name;
                itm.Tag = item;
                listBox.Items.Add(itm);
            }
        }

        private void loadListBoxScenario()
        {
            listBoxScenario.Items.Clear();
            List<Scenario> list = new List<Scenario>();
            using (var ouw = GetOuw())
            {
                var repo = ouw.Repository<Scenario>();
                var data = repo.FindAll();
                list = data.ToList();
            }
            foreach (var item in list)
            {
                ListBoxItem itm = new ListBoxItem();
                itm.Content = item.Name;
                itm.Tag = item;
                listBoxScenario.Items.Add(itm);
            }
        }
        private void loadListBoxActor()
        {
            listBoxActor.Items.Clear();
            //listBoxActor2.Items.Clear();
            List<Actor> list = new List<Actor>();
            using (var ouw = GetOuw())
            {
                var repo = ouw.Repository<Actor>();
                var data = repo.FindAll();
                list = data.ToList();
            }
            foreach (var item in list)
            {
                ListBoxItem itm = new ListBoxItem();
                itm.Content = item.Name;
                itm.Tag = item;
                listBoxActor.Items.Add(itm);
                //listBoxActor2.Items.Add(itm);
            }
        }

        private void loadListBoxHabit()
        {
            listBoxHabit.Items.Clear();
            List<Habit> list = new List<Habit>();
            using (var ouw = GetOuw())
            {
                var repo = ouw.Repository<Habit>();
                var data = repo.FindAll();
                if (data == null)
                    return;
                list = data.ToList();
            }
            foreach (var item in list)
            {
                ListBoxItem itm = new ListBoxItem();
                itm.Content = item.Name;
                itm.Tag = item;
                listBoxHabit.Items.Add(itm);
            }
        }




        #endregion Private Methods

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            var d = SimulationObjects.Device.Devices.find("10.0.0.13");
            ((SimulationObjects.Device.Light)d).Open("10.0.0.13");
            System.Threading.Thread.Sleep(10000);
            ((SimulationObjects.Device.Light)d).Close("10.0.0.13");

            return;

            //TODO=> manifestten çek veya belli bir dizindeki bütün dll'lerden çekerek yapılabilir, path manifestten çekilir.
            // Assembly.Load (LoadFile değil): zaten yüklü assembly'yi döndürür, böylece
            // Device.Devices gibi statik registry'ler çağıran tarafla aynı kopyayı paylaşır.
            Assembly asm = Assembly.Load("SimulationObjects");

            if (asm == null)
                throw new Exception(); //TODO: Throw Exception assebly bulunamadı

            var type = asm.GetTypes().Where(x => x.Name == "Light").FirstOrDefault();
            if (type == null)
                throw new Exception(); //TODO: Throw Exception cihaz bulunamadı

            var obje = Activator.CreateInstance(type);
            if (obje == null)
                throw new Exception(); //TODO: Throw Exception 



            bool state = false;
            for (int i = 0; i < 10; i++)
            {



                if (state)
                {
                    //((SimulationObjects.Device.Light)device).Close("10.0.0.2");
                    var mInfo = type.GetMethod("Open");
                    if (mInfo == null)
                        throw new Exception(); //TODO: Throw Exception method bulunamadı

                    mInfo.Invoke(obje, new object[] { "10.0.0.13" });

                }
                else
                {
                    //((SimulationObjects.Device.Light)device).Open("10.0.0.2");
                    var mInfo = type.GetMethod("Close");
                    if (mInfo == null)
                        throw new Exception(); //TODO: Throw Exception method bulunamadı

                    mInfo.Invoke(obje, new object[] { "10.0.0.13" });
                }

                System.Threading.Thread.Sleep(5000);
                state = !state;

            }
        }
    }
}

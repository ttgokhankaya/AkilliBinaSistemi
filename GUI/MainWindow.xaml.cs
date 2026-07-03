using FieldModel;
using SharedObject;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SCU _scu;
        Timer _timer;
        public ObservableCollection<AdleMemoryObject> CaughtedMoments { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _scu = SCU.GetSCU();
            if (_scu.InitSystem())
                MessageBox.Show("Kontrol Birimi Başladı");

            CaughtedMoments = new ObservableCollection<AdleMemoryObject>();

            _timer = new Timer();
            _timer.Interval = 5000;
            _timer.Elapsed += _timer_Elapsed;

            this.DataContext = this;
            TreeViewDoldur();
            ItemsListDoldur();
        }

        bool timerOn = false;
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (timerOn)
            {
                return;
            }

            timerOn = true;

            var memoriesThread = Task.Factory.StartNew(() => { return _scu.MemoryManager.GetAllMemories(); });

            memoriesThread.Wait();
            var memories = memoriesThread.Result;

            for (int i = 0; i < memories.Count; i++)
            {
                var foundMemory = CaughtedMoments.FirstOrDefault(x => x.Key == memories[i].Key);
                if (foundMemory != null)
                    continue;

                App.Current.Dispatcher.Invoke(() =>
                {
                    CaughtedMoments.Add(memories[i]);
                });
            }

            timerOn = false;
        }

        private void ItemsListDoldur()
        {
            listbox_Items.Items.Clear();

            foreach (var item in _scu.KnownItems)
            {
                ListBoxItem listBoxItem = new ListBoxItem();
                listBoxItem.Content = item.Name;
                listBoxItem.Tag = item;
                listbox_Items.Items.Add(listBoxItem);
            }
        }

        private void TreeViewDoldur()
        {
            treeView.Items.Clear();

            List<TreeViewItem> menuList = new List<TreeViewItem>();

            foreach (var area in _scu.Areas)
            {
                if (area.RootArea != null)
                    continue;

                TreeViewItem item = new TreeViewItem();
                item.Header = area.Name;
                item.Tag = area;

                if (area.AreaHasItems)
                {
                    foreach (var areaItemDefinition in area.Items)
                    {
                        TreeViewItem areaitem = new TreeViewItem();
                        areaitem.Header = $"{areaItemDefinition.GetType().Name} {areaItemDefinition.IpV4} - {areaItemDefinition.Name}";
                        areaitem.Tag = areaItemDefinition;
                        item.Items.Add(areaitem);
                    }
                }

                if (area.AreaHasSubAreas)
                {
                    AddToChild(ref item, area);
                }
                menuList.Add(item);
            }

            foreach (var item in menuList)
            {
                treeView.Items.Add(item);
            }
        }

        private void AddToChild(ref TreeViewItem item, AdleAreaBase area)
        {
            foreach (var subArea in area.SubAreas)
            {
                TreeViewItem child = new TreeViewItem();
                child.Header = subArea.Name;
                child.Tag = subArea;

                if (subArea.AreaHasSubAreas)
                    AddToChild(ref child, subArea);

                item.Items.Add(child);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            AdleAreaBase selectedOne = null;
            if (treeView.SelectedItem != null)
                selectedOne = (AdleAreaBase)((TreeViewItem)treeView.SelectedItem).Tag;

            AdleAreaBase newAreaDefinition = new HouseArea()
            {
                Manager = _scu,
                Name = txtAreaName.Text,
                SubAreas = null,
                RootArea = selectedOne,

                Height = 0,
                Width = 0
            };
            _scu.RegisterArea(newAreaDefinition);

            TreeViewDoldur();
            return;
        }

        private void button_NesneEkle_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtIP.Text))
            {
                MessageBox.Show("Lütfen IP giriniz.");
                return;
            }

            if (listbox_Items.SelectedItem == null)
            {
                MessageBox.Show("Lütfen nesne seçiniz.");
                return;
            }

            if (treeView.SelectedItem == null)
            {
                MessageBox.Show("Lütfen alan seçiniz.");
                return;
            }

            var selectedItem = GetSelectedItem();
            var selectedArea = GetSelectedArea();

            if (selectedItem == null)
            {
                MessageBox.Show("Nesne seçiminde hata oluştu. Lütfen tekrar deneyiniz.");
                return;
            }

            if (selectedArea == null)
            {
                MessageBox.Show("Alan seçiminde hata oluştu. Lütfen tekrar deneyiniz.");
                return;
            }

            var instance = (AdleItemBase)Activator.CreateInstance(selectedItem);
            instance.IpV4 = txtIP.Text;
            _scu.RegisterItem(selectedArea, instance);

            TreeViewDoldur();
            MessageBox.Show($"{selectedItem.Name} nesnesi {selectedArea.Name} alanına {txtIP.Text} tanımı ile eklendi.\nİşlem tamamlandı.");
        }

        private Type GetSelectedItem()
        {
            if (listbox_Items.SelectedItem == null)
                return null;

            return (Type)((ListBoxItem)listbox_Items.SelectedItem).Tag;
        }

        private AdleAreaBase GetSelectedArea()
        {
            if (treeView.SelectedItem == null)
                return null; ;

            return (AdleAreaBase)((TreeViewItem)treeView.SelectedItem).Tag;
        }

        private void listbox_Items_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listbox_Items.SelectedItem == null)
            {
                return;
            }

            var type = (Type)((ListBoxItem)listbox_Items.SelectedItem).Tag;

            FillActionList(type);
        }

        private void FillActionList(Type type, AdleItemBase baseItem = null)
        {
            var list = _scu.GetActionableCommands(type);
            listbox_Items_actions.Items.Clear();

            foreach (var item in list)
            {
                ItemBarUserControl bar = new ItemBarUserControl();
                bar.SetAction(item, baseItem);
                bar.executeItem += Bar_executeItem;
                ListBoxItem newItem = new ListBoxItem();
                newItem.Content = bar;
                listbox_Items_actions.Items.Add(newItem);
            }
        }

        private void Bar_executeItem(object sender, ExecuteItemEventArgs e)
        {
            if (e.Item == null || string.IsNullOrEmpty(e.ActionName)) return;
            _scu.PlaceCommand(e.Item, e.ActionName);
        }

        private void btnBasla_Click(object sender, RoutedEventArgs e)
        {
            _scu.BeginLister();
            _timer.Start();
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeView.SelectedItem == null) return;

            if (!(((TreeViewItem)treeView.SelectedItem).Tag is AdleItemBase)) return;

            var item = (AdleItemBase)((TreeViewItem)treeView.SelectedItem).Tag;
            FillActionList(item.GetType(), item);
        }

        private void btnCalistir_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDurdur_Click(object sender, RoutedEventArgs e)
        {
            _scu.Stop();
            _timer.Stop();
        }
    }
}

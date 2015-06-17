using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidApp.Adapters;
using AndroidApp.Core;
using AndroidApp.Fragments;
using Microsoft.WindowsAzure.MobileServices;

namespace AndroidApp.Screens
{
    [Activity(Label = "Edit Scribe Data")]
    public class EditDataScribeActivity : Activity
    {
        // Mobile Service Client Reference
        private MobileServiceClient client;

        // Mobile Service Tables Used To Access Data
        private IMobileServiceTable<ReminderItem> reminderTable;

        // Adapter To Sync Reminders With The List
        private ReminderAdapter reminderAdapter;

        // Date Selection Variables
        private DateTime selectedDate;
        private Button datePickerButton;

        // Reminder Button
        private Button addReminderButton;

        // Meal Buttons
        private Button setBreakfastButton;
        private Button setLunchButton;
        private Button setDinnerButton;

        // Save Button
        private Button saveButton;

        /** Create Activity **/
        protected override async void OnCreate(Bundle bundle)
        {         
            // Create And Display Layout
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.EditDataScribeActivity);

            // Date Picker Button And Functionality
            datePickerButton = FindViewById<Button>(Resource.Id.buttonDatePicker);
            datePickerButton.Click += delegate { ShowDialog(0); };
            selectedDate = DateTime.Today;;
            datePickerButton.Text = selectedDate.ToShortDateString();

            // Add Reminder Button
            addReminderButton = FindViewById<Button>(Resource.Id.buttonAddReminder);
            addReminderButton.Click += (object sender, EventArgs e) =>
            {
                CreateAndShowAddReminderDialog();
            };

            // Add Save Button
            saveButton = FindViewById<Button>(Resource.Id.buttonSave);
            saveButton.Click += (object sender, EventArgs e) =>
            {
                // TODO: Add Progress Bar For Saving Data
                // TODO: Possible Confirmation Dialog

                // Start With Reminders
                for (int i = 0; i < reminderAdapter.Count; i++)
                {
                    if (reminderAdapter[i].Id == null)
                        AddReminderItem(reminderAdapter[i]);
                }
            };

            // Connect To Azure Mobile Service
            try
            {
                // Initialize
                CurrentPlatform.Init();

                // Create Mobile Service Client Instance
                client = new MobileServiceClient(Constants.APPLICATION_URL, Constants.APPLICATION_KEY);

                // Retrieve Tables
                reminderTable = client.GetTable<ReminderItem>();

                // Create Adapter To Bind The Reminder Items To The View
                reminderAdapter = new ReminderAdapter(this);
                var reminderListView = FindViewById<ListView>(Resource.Id.listViewRemindersScribe);
                reminderListView.Adapter = reminderAdapter;

                // Load The Reminders From The Mobile Service
                await RefreshRemindersFromTableAsync(DateTime.Today);

            }
            catch (Exception e)
            {
                CreateAndShowDialog(e, "Connection Error");
            }
        }

        /** Azure Mobile Service Methods **/
        async void OnRefreshRemindersFromTable(DateTime date)
        {
            await RefreshRemindersFromTableAsync(date);
        }
    
        async Task RefreshRemindersFromTableAsync(DateTime date)
        {
            try
            {
                var list = await reminderTable.Where(x => x.Date.Day == date.Day).ToListAsync();

                reminderAdapter.Clear();

                foreach (ReminderItem current in list)
                    reminderAdapter.Add(current);

            }
            catch (Exception e)
            {
                CreateAndShowDialog(e, "Connection Error");
            }
        }

        public async void AddReminderItem(ReminderItem item)
        {
            try
            {
                await reminderTable.InsertAsync(item);
            }
            catch (Exception e)
            {
                CreateAndShowDialog(e, "Unable To Insert Reminder");
            }
        }

        public async void RemoveReminderItem(ReminderItem item)
        {
            try
            {
                reminderTable.DeleteAsync(item);
            }
            catch (Exception e)
            {
                CreateAndShowDialog(e, "Unable To Remove Reminder");
            }

            reminderAdapter.Remove(item);
        }

        /** Add Reminder Dialog **/
        void CreateAndShowAddReminderDialog()
        {
            // Create Dialog And Transaction
            var transaction = FragmentManager.BeginTransaction();
            var reminderDialog = new AddReminderDialogFragment();

            // Probably Horrible Practice
            reminderDialog.date = selectedDate;
            reminderDialog.reminderTable = reminderTable;
            reminderDialog.reminderAdapter = reminderAdapter;
            reminderDialog.Show(transaction, "addReminderDialog");
        }

        /** Date Picking Methods **/
        protected override Dialog OnCreateDialog(int id)
        {
            return new DatePickerDialog(this, HandleDateSet, selectedDate.Year, selectedDate.Month - 1, selectedDate.Day);
        }

        // TODO: Handle Returning To Same Date
        void HandleDateSet(object sender, DatePickerDialog.DateSetEventArgs e)
        {
            reminderAdapter.Clear();
            selectedDate = e.Date;
            datePickerButton.Text = selectedDate.ToShortDateString();
            OnRefreshRemindersFromTable(e.Date);
        }

        /** Error Dialog **/
        void CreateAndShowDialog(Exception exception, String title)
        {
            CreateAndShowDialog(exception.Message, title);
        }

        void CreateAndShowDialog(string message, string title)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);

            builder.SetMessage(message);
            builder.SetTitle(title);
            builder.Create().Show();
        }
    }
}
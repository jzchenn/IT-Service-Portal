// <copyright file="MainWindow.xaml.cs" company="Rangitoto College">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
//
// Program: TicketingPRGInternalAssessment
// Author: Jacky Chen
// Created: May 28th 2025
// Description: This file contains the logic for the main window of the WPF ticketing application. It handles login, ticket creation and ticket management.
// The application communicates with a MySQL database to store and retrieve ticket information.

namespace TicketingPRGInternalAssessment
{
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics.Eventing.Reader;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using MySql.Data.MySqlClient;
    using MySqlX.XDevAPI;

    /// <summary>
    /// The main window of the ticketing system.
    /// Handles login, ticket creation, and admin ticket management.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Stores the ID of the currently logged-in user.
        /// </summary>
        private int loggedInUserId;

        /// <summary>
        /// Special marker value used for placeholder or no value change rows in combo boxes.
        /// </summary>
        private int noSelectionValue;

        /// <summary>
        /// SQL query used to select tickets for display in the DataGrid.
        /// </summary>
        private string selectTicketsQuery;

        /// <summary>
        /// Stores the 'open' status Id number.
        /// </summary>
        private int openStatusId;

        /// <summary>
        /// The minimum text length for detailed description and brief summary.
        /// </summary>
        private int minTextLength;

        /// <summary>
        /// Special marker value used for unassigned tickets in combo boxes.
        /// </summary>
        private int unassignedTicketValue;

        /// <summary>
        /// Connection to the MySQL database.
        /// </summary>
        private MySqlConnection connection;

        // Main Window

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// Sets up database connection and loads UI data.
        /// </summary>
        public MainWindow()
        {
            this.noSelectionValue = -1;
            this.unassignedTicketValue = 0;
            this.selectTicketsQuery = "SELECT ticket_id, ticket_statuses.status_name as CurrentStatus , a2.username AS AssignedAdmin, categories.category_name AS IssueCategory, brief_summary AS BriefSummary, accounts.username as TicketCreator, roles.role_name as CreatorRole " +
                "FROM tickets LEFT JOIN accounts ON tickets.creator_account_id = accounts.account_id " +
                "LEFT JOIN accounts a2 ON tickets.assigned_account_id = a2.account_id " +
                "LEFT JOIN ticket_statuses ON tickets.status_id = ticket_statuses.status_id " +
                "LEFT JOIN categories ON tickets.category_id = categories.category_id LEFT JOIN roles ON accounts.role_id = roles.role_id";
            this.minTextLength = 8;
            this.InitializeComponent();
            string connectionString =
                "Server=;" +
                "Port=;" +
                "database=;" +
                "UID=;" +
                "password=;";
            this.connection = new MySqlConnection(connectionString);
            this.connection.Open();
            var getOpenStatusId = new MySqlCommand(
                $"SELECT status_id FROM ticket_statuses WHERE status_name = 'Open'", this.connection);
            this.openStatusId = Convert.ToInt32(getOpenStatusId.ExecuteScalar());
            this.SummaryTextCount.Content = $"{this.BriefTextBox.Text.Length}/{this.BriefTextBox.MaxLength}";
            this.DescriptionTextCount.Content = $"{this.DetailedTextBox.Text.Length}/{this.DetailedTextBox.MaxLength}";
        }

        /// <summary>
        /// Loads ticket data from the database and displays it in the DataGrid.
        /// </summary>
        private void FillDataGrid()
        {
            try
            {
                string query = this.selectTicketsQuery;
                if (this.FilterComboBox.SelectedValue != null)
                {
                    if ((int)this.FilterComboBox.SelectedValue != this.unassignedTicketValue && (int)this.FilterComboBox.SelectedValue != this.noSelectionValue)
                    {
                        query += $" WHERE a2.account_id = @accountID ";
                    }

                    if ((int)this.FilterComboBox.SelectedValue == this.unassignedTicketValue)
                    {
                        query += $" WHERE a2.account_id IS NULL";
                    }
                }

                var fillGrid = new MySqlCommand(query, this.connection);

                if (this.FilterComboBox.SelectedValue != null)
                {
                    if ((int)this.FilterComboBox.SelectedValue != this.unassignedTicketValue && (int)this.FilterComboBox.SelectedValue != this.noSelectionValue)
                    {
                        fillGrid.Parameters.AddWithValue("@accountID", this.FilterComboBox.SelectedValue);
                    }
                }

                DataTable dataTable = new DataTable();
                MySqlDataAdapter dataAdapter = new MySqlDataAdapter(fillGrid);
                dataAdapter.Fill(dataTable);
                this.TicketDataGrid.DataContext = dataTable;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        // Login Screen

        /// <summary>
        /// Handles the login process when the login button is clicked.
        /// Validates user credentials and determines user role (Admin or Teacher/Students).
        /// </summary>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var loginCheckcmd = new MySqlCommand(
            $"SELECT account_id FROM accounts WHERE accounts.username=@username AND accounts.userpassword=SHA2(@password, 256);", this.connection);
            loginCheckcmd.Parameters.AddWithValue("@username", this.UsernameTextBox.Text);
            loginCheckcmd.Parameters.AddWithValue("@password", this.PasswordTextBox.Password.ToString());
            int? accountID = (int?)loginCheckcmd.ExecuteScalar();
            if (!accountID.HasValue)
            {
                MessageBox.Show("Wrong username or password");
            }
            else
            {
                this.loggedInUserId = Convert.ToInt32(accountID);
                MessageBox.Show($"Welcome {this.UsernameTextBox.Text}!");
                var roleCheckcmd = new MySqlCommand(
                $"SELECT role_name FROM roles INNER JOIN accounts ON roles.role_id = accounts.role_id WHERE accounts.account_id = @accountID;", this.connection);
                roleCheckcmd.Parameters.AddWithValue("@accountID", accountID);
                string userRole = (string)roleCheckcmd.ExecuteScalar();
                if (userRole == "Admin")
                {
                    this.FillDataGrid();
                    this.LoadStaffComboBox();
                    this.TabControl.SelectedIndex = 2;
                }
                else
                {
                    this.LoadNonStaffComboBox();
                    this.TabControl.SelectedIndex = 1;
                }
            }
        }

        // Ticket Create Screen

        /// <summary>
        /// Loads category options for non-admin users for creating tickets.
        /// </summary>
        private void LoadNonStaffComboBox()
        {
            // 0 is the default/first option in the combo box.
            using (MySqlCommand selectCategoryTable = new MySqlCommand("Select category_id, category_name FROM categories", this.connection))
            using (MySqlDataReader rd = selectCategoryTable.ExecuteReader())
            {
                {
                    DataTable dt = new DataTable();
                    dt.Load(rd);

                    // Add extra options outside of the database.
                    DataRow dr = dt.NewRow();
                    dr["category_id"] = this.noSelectionValue;
                    dr["category_name"] = "-- Click on this combo box to select an option --"; // Placeholder text.
                    dt.Rows.InsertAt(dr, 0); // Inserts as the first option.

                    // Adds each category from database to combo box as an option.
                    this.CategoryComboBox.ItemsSource = dt.DefaultView;
                    this.CategoryComboBox.DisplayMemberPath = "category_name";
                    this.CategoryComboBox.SelectedValuePath = "category_id";
                    this.CategoryComboBox.SelectedIndex = 0; // Display the first combo box option.
                }
            }
        }

        /// <summary>
        /// Handles changes in the combo box selection for ticket creation.
        /// If there is a change in combo box selection then placeholder row is deleted.
        /// </summary>
        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 0 is the default/first option in the combo box.
            if (this.CategoryComboBox.SelectedIndex == 0) // Prevents the the removal Placeholder text at the beginning.
            {
                return;
            }

            if (Convert.ToInt32(this.CategoryComboBox.SelectedValue) != this.noSelectionValue)
            {
                DataView? dv = this.CategoryComboBox.ItemsSource as DataView;
                if (dv != null && dv.Table?.Rows.Count > 0 && dv.Table.Rows[0]["category_id"].ToString() == this.noSelectionValue.ToString())
                {
                    dv.Table.Rows.RemoveAt(0); // Removes the placeholder text.
                }
            }
        }

        /// <summary>
        /// Submits a new ticket to the database if all fields are valid.
        /// </summary>
        private void SubmitTicketButton_Click(object sender, RoutedEventArgs e)
        {
            int briefSummaryLength = this.BriefTextBox.Text.Length;
            int detailedDescriptionLength = this.DetailedTextBox.Text.Length;
            if (Convert.ToInt32(this.CategoryComboBox.SelectedValue) != this.noSelectionValue)
            {
                try
                {
                    int categoryID = Convert.ToInt32(this.CategoryComboBox.SelectedValue);

                    if (briefSummaryLength >= this.minTextLength && detailedDescriptionLength >= this.minTextLength)
                    {
                        var ticketSubmitcmd = new MySqlCommand(
                            $"INSERT INTO `tickets` ( creator_account_id, category_id, status_id, brief_summary, detailed_description) " +
                            $"VALUES ( @userID, @category, @status, @brief, @detailed)", this.connection);
                        ticketSubmitcmd.Parameters.AddWithValue("@userID", this.loggedInUserId);
                        ticketSubmitcmd.Parameters.AddWithValue("@category", categoryID);
                        ticketSubmitcmd.Parameters.AddWithValue("@status", this.openStatusId);
                        ticketSubmitcmd.Parameters.AddWithValue("@brief", this.BriefTextBox.Text);
                        ticketSubmitcmd.Parameters.AddWithValue("@detailed", this.DetailedTextBox.Text);
                        ticketSubmitcmd.ExecuteNonQuery();
                        MessageBox.Show("Ticket submitted!");
                        this.BriefTextBox.Text = null;
                        this.DetailedTextBox.Text = null;
                    }
                    else
                    {
                        MessageBox.Show($"The brief summary and detailed description must be at least {this.minTextLength} characters.");
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show($"{ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please choose a category!");
            }
        }

        /// <summary>
        /// Updates the character counter and validation color for the brief summary field.
        /// </summary>
        private void BriefTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int briefTextCount = this.BriefTextBox.Text.Length;
            string charCount = briefTextCount.ToString() + "/" + this.BriefTextBox.MaxLength.ToString();
            this.SummaryTextCount.Content = charCount;
            if (briefTextCount < this.minTextLength)
            {
                this.SummaryTextCount.Foreground = Brushes.Red;
            }
            else
            {
                this.SummaryTextCount.Foreground = Brushes.Green;
            }
        }

        /// <summary>
        /// Updates the character counter and validation color for the detailed description field.
        /// </summary>
        private void DetailedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int detailedTextCount = this.DetailedTextBox.Text.Length;
            string charCount = detailedTextCount.ToString() + "/" + this.DetailedTextBox.MaxLength.ToString();
            this.DescriptionTextCount.Content = charCount;
            if (detailedTextCount < this.minTextLength)
            {
                this.DescriptionTextCount.Foreground = Brushes.Red;
            }
            else
            {
                this.DescriptionTextCount.Foreground = Brushes.Green;
            }
        }

        // Ticket Manage Screen

        /// <summary>
        /// Loads combo box data for admins: categories, staff assignments, statuses, and filters.
        /// </summary>
        private void LoadStaffComboBox()
        {
            // 0 is the default/first option in the combo box.
            using (MySqlCommand selectCategoryTable = new MySqlCommand("Select category_id, category_name FROM categories", this.connection))
            using (MySqlDataReader rd = selectCategoryTable.ExecuteReader())
            {
                {
                    DataTable dt = new DataTable();
                    dt.Load(rd);

                    DataRow dr1 = dt.NewRow();
                    dr1["category_id"] = this.noSelectionValue;
                    dr1["category_name"] = "(No Change)";
                    dt.Rows.InsertAt(dr1, 0); // Inserts the new row as the first option.

                    this.ChangeCategoryComboBox.ItemsSource = dt.DefaultView;
                    this.ChangeCategoryComboBox.DisplayMemberPath = "category_name";
                    this.ChangeCategoryComboBox.SelectedValuePath = "category_id";
                    this.ChangeCategoryComboBox.SelectedIndex = 0; // Display the first option.
                }
            }

            using (MySqlCommand selectAccountTable = new MySqlCommand("SELECT account_id, username FROM accounts INNER JOIN roles ON accounts.role_id = roles.role_id WHERE roles.role_name = 'Admin' ", this.connection))
            using (MySqlDataReader rd = selectAccountTable.ExecuteReader())
            {
                // 0 is the default/first option in the combo box.
                {
                    DataTable dt = new DataTable();
                    dt.Load(rd);

                    DataRow dr2 = dt.NewRow();
                    dr2["account_id"] = this.noSelectionValue;
                    dr2["username"] = "(No Change)";
                    dt.Rows.InsertAt(dr2, 0); // Inserts the new row as the first option.

                    this.AssignStaffComboBox.ItemsSource = dt.DefaultView;
                    this.AssignStaffComboBox.DisplayMemberPath = "username";
                    this.AssignStaffComboBox.SelectedValuePath = "account_id";
                    this.AssignStaffComboBox.SelectedIndex = 0; // Display the first option.
                }
            }

            using (MySqlCommand selectStatusTable = new MySqlCommand("Select status_id, status_name FROM ticket_statuses", this.connection))
            using (MySqlDataReader rd = selectStatusTable.ExecuteReader())
            {
                // 0 is the default/first option in the combo box.
                {
                    DataTable dt = new DataTable();
                    dt.Load(rd);

                    DataRow dr = dt.NewRow();
                    dr["status_id"] = this.noSelectionValue;
                    dr["status_name"] = "(No Change)";
                    dt.Rows.InsertAt(dr, 0); // Inserts the new row as the first option.

                    this.StatusUpdateComboBox.ItemsSource = dt.DefaultView;
                    this.StatusUpdateComboBox.DisplayMemberPath = "status_name";
                    this.StatusUpdateComboBox.SelectedValuePath = "status_id";
                    this.StatusUpdateComboBox.SelectedIndex = 0; // Display the first option.
                }
            }

            using (MySqlCommand selectAccountTable = new MySqlCommand("SELECT account_id, username FROM accounts INNER JOIN roles ON accounts.role_id = roles.role_id WHERE roles.role_name = 'Admin' ", this.connection))
            using (MySqlDataReader rd = selectAccountTable.ExecuteReader())
            {
                {
                    DataTable dt = new DataTable();
                    dt.Load(rd);

                    DataRow dr = dt.NewRow();
                    dr["account_id"] = this.unassignedTicketValue;
                    dr["username"] = "Unassigned Tickets";
                    dt.Rows.InsertAt(dr, 0); // Inserts the new row as the first option.
                    DataRow dr2 = dt.NewRow();
                    dr2["account_id"] = this.noSelectionValue;
                    dr2["username"] = "(No filter)";

                    // Since (No filter) is insertted after Unassigned Tickets, Unassigned tickets becomes the second option.
                    dt.Rows.InsertAt(dr2, 0); // Inserts the new row as the first option.

                    this.FilterComboBox.ItemsSource = dt.DefaultView;
                    this.FilterComboBox.DisplayMemberPath = "username";
                    this.FilterComboBox.SelectedValuePath = "account_id";
                    this.FilterComboBox.SelectedIndex = 0; // Display the first option, (No filter).
                }
            }
        }

        /// <summary>
        /// Assigns or updates ticket details (staff, category, status) for the selected ticket.
        /// </summary>
        private void AssignButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.TicketDataGrid.SelectedItems.Count > 0) // 0 means at least one row is selected.
            {
                try
                {
                    string? ticketID = ((DataRowView?)this.TicketDataGrid.SelectedItems[0] as DataRowView)?["ticket_id"].ToString();
                    int adminID = Convert.ToInt32(this.AssignStaffComboBox.SelectedValue);
                    int newCategoryID = Convert.ToInt32(this.ChangeCategoryComboBox.SelectedValue);
                    int statusUpdateID = Convert.ToInt32(this.StatusUpdateComboBox.SelectedValue);
                    if (adminID == this.noSelectionValue && newCategoryID == this.noSelectionValue && statusUpdateID == this.noSelectionValue)
                    {
                        MessageBox.Show("No changes were made.");
                    }
                    else
                    {
                        MessageBoxResult updateTicketConfirmation = MessageBox.Show($"Are you sure you want to update the ticket with the ticket id: {ticketID}?", "Change Confirmation", MessageBoxButton.YesNo);
                        if (updateTicketConfirmation == MessageBoxResult.Yes)
                        {
                            if ((int)this.AssignStaffComboBox.SelectedValue != this.noSelectionValue)
                            {
                                var assignAdmin = new MySqlCommand($"UPDATE `tickets` SET assigned_account_id=@adminID WHERE (`ticket_id` = @ticketID);", this.connection);
                                assignAdmin.Parameters.AddWithValue("@adminID", adminID);
                                assignAdmin.Parameters.AddWithValue("@ticketID", ticketID);
                                assignAdmin.ExecuteNonQuery();
                            }

                            if ((int)this.ChangeCategoryComboBox.SelectedValue != this.noSelectionValue)
                            {
                                var reassignCategory = new MySqlCommand($"UPDATE `tickets` SET category_id=@newCategoryID WHERE (`ticket_id` = @ticketID);", this.connection);
                                reassignCategory.Parameters.AddWithValue("@newCategoryID", newCategoryID);
                                reassignCategory.Parameters.AddWithValue("@ticketID", ticketID);
                                reassignCategory.ExecuteNonQuery();
                            }

                            if ((int)this.StatusUpdateComboBox.SelectedValue != this.noSelectionValue)
                            {
                                var reassignStatus = new MySqlCommand($"UPDATE `tickets` SET status_id=@statusUpdateID WHERE (`ticket_id` = @ticketID);", this.connection);
                                reassignStatus.Parameters.AddWithValue("@statusUpdateID", statusUpdateID);
                                reassignStatus.Parameters.AddWithValue("@ticketID", ticketID);
                                reassignStatus.ExecuteNonQuery();
                            }

                            this.FillDataGrid();
                            MessageBoxResult resetComboBoxSelection = MessageBox.Show($"Successfully updated ticket! \nWould you like to reset combo boxes selection?", "Change Confirmation", MessageBoxButton.YesNo);
                            if (resetComboBoxSelection == MessageBoxResult.Yes)
                            {
                                // 0 makes the following combo boxes reset back to the first option, (No filter) in this case.
                                this.AssignStaffComboBox.SelectedIndex = 0;
                                this.ChangeCategoryComboBox.SelectedIndex = 0;
                                this.StatusUpdateComboBox.SelectedIndex = 0;
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
        }

        /// <summary>
        /// Deletes the selected ticket from the database after user confirmation.
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
            if (this.TicketDataGrid.SelectedItems.Count > 0) // 0 means at least one row is selected.
            {
                try
                {
                    string? ticketID = ((DataRowView?)this.TicketDataGrid.SelectedItems[0] as DataRowView)?["ticket_id"].ToString();
                    MessageBoxResult deleteTicketConfirmation = MessageBox.Show($"Are you sure you want to delete the ticket with the ticket ID: {ticketID}?", "Delete Confirmation", MessageBoxButton.YesNo);
                    if (deleteTicketConfirmation == MessageBoxResult.Yes)
                    {
                        var deleteTicket = new MySqlCommand($"DELETE FROM tickets WHERE (`ticket_id` = @ticketID);", this.connection);
                        deleteTicket.Parameters.AddWithValue("@ticketID", ticketID);
                        deleteTicket.ExecuteNonQuery();
                        this.FillDataGrid();
                        this.DetailedSummaryText.Text = null;
                        MessageBox.Show("Ticket Deleted!");
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
        }

        /// <summary>
        /// Displays the full detailed description of the currently selected ticket.
        /// </summary>
        private void TicketDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.TicketDataGrid.SelectedItems.Count > 0) // 0 means at least one row is selected.
            {
                try
                {
                    string? ticketID = ((DataRowView?)this.TicketDataGrid.SelectedItems[0] as DataRowView)?["ticket_id"].ToString();
                    var getDescription = new MySqlCommand($"SELECT detailed_description FROM tickets WHERE (`ticket_id` = @ticketID);", this.connection);
                    getDescription.Parameters.AddWithValue("@ticketID", ticketID);
                    string description = (string)getDescription.ExecuteScalar();
                    this.DetailedSummaryText.Text = description;
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
        }

        /// <summary>
        /// Applies a filter to the DataGrid based on the selected staff member or unassigned option.
        /// </summary>
        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.FillDataGrid();
        }
    }
}
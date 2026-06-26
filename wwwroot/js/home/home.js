// HomePage - Organized JavaScript for Home Controller views
// Structure: index (Kiosk scanning), historyLogs (History logs with export)

const HomePage = {
    // Common utilities
    common: {
        showSuccess: function(message, title = 'Success') {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: title,
                    text: message,
                    timer: 2000,
                    showConfirmButton: false
                });
            }
        },

        showError: function(message, title = 'Error') {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'error',
                    title: title,
                    text: message,
                    timer: 3000,
                    showConfirmButton: false
                });
            }
        },

        showWarning: function(message, title = 'Warning') {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'warning',
                    title: title,
                    text: message,
                    timer: 2000,
                    showConfirmButton: false
                });
            }
        },

        formatDate: function(dateString) {
            if (!dateString) return '';
            const date = new Date(dateString);
            return date.toLocaleString();
        },

        formatDateTime: function(dateString) {
            if (!dateString) return '';
            const date = new Date(dateString);
            return date.toISOString().slice(0, 19).replace('T', ' ');
        }
    },

    // Index.cshtml - Kiosk scanning functionality
    index: {
        // Global variables for health declaration timer
        healthDeclarationTimer: null,
        timerCountdown: 120,

        init: function() {
            const self = this;

            // Set focus on employee_id input
            $('#employee_id').focus();

            // ID Scanning - Enter key processing
            $('#employee_id').on('keypress', function(e) {
                if (e.which === 13) { // Enter key
                    e.preventDefault();
                    self.processScan();
                }
            });

            // Health declaration radio button change handler
            $('input[name="health_status"]').on('change', function() {
                const selectedStatus = $(this).val();
                const attendanceId = $('#hd_attendance_id').val();

                if (selectedStatus === 'UNFIT') {
                    // Show blocking message for UNFIT
                    HomePage.common.showError('You are not allowed to enter company premises', 'Not Allowed');

                    // Update health status to UNFIT (sets TIME OUT)
                    self.updateHealthStatus(attendanceId, 'UNFIT');
                } else if (selectedStatus === 'FIT') {
                    // Update health status to FIT (removes TIME OUT if exists)
                    self.updateHealthStatus(attendanceId, 'FIT');
                }
            });
        },

        processScan: function() {
            const self = this;
            const employeeId = $('#employee_id').val().trim();
            if (!employeeId) return;

            // Clear any existing health declaration timer
            self.clearHealthDeclarationTimer();

            $.ajax({
                url: '/Home/Scan',
                method: 'POST',
                data: { employee_id: employeeId },
                success: function(response) {
                    if (response.success) {
                        self.displayContractorInfo(response.data.contractor_info);
                        self.displayScanResult(response.message);

                        // Show health declaration form for this contractor
                        self.showHealthDeclarationForm(response.data.attendance_id);

                        // Clear input for next scan
                        $('#employee_id').val('');
                        $('#employee_id').focus();
                    } else {
                        self.displayScanError(response.message);
                    }
                },
                error: function() {
                    HomePage.common.showError('Failed to process scan');
                }
            });
        },

        displayContractorInfo: function(contractor) {
            if (!contractor) {
                $('#contractor-info').hide();
                return;
            }

            $('#employee_name').text('Name: ' + contractor.name);
            $('#provider').text('Provider: ' + contractor.provider_code);
            $('#position').text('Position: ' + contractor.position);
            $('#area_of_destination').text('Area: ' + contractor.area_of_destination);
            $('#contractor-info').show();
        },

        displayScanResult: function(message) {
            HomePage.common.showSuccess(message, 'Scan Successful');
        },

        displayScanError: function(message) {
            HomePage.common.showError(message, 'Scan Failed');

            // Clear contractor info on error
            $('#contractor-info').hide();
        },

        showHealthDeclarationForm: function(attendanceId) {
            $('#hd_attendance_id').val(attendanceId);
            $('#health-declaration-section').show();
            $('#no-declaration-message').hide();

            // Reset form to default FIT status
            $('#health_fit').prop('checked', true);

            // Start 2-minute timer
            this.startHealthDeclarationTimer();
        },

        hideHealthDeclarationForm: function() {
            $('#health-declaration-section').hide();
            $('#no-declaration-message').show();
            $('#hd_attendance_id').val('');
            this.clearHealthDeclarationTimer();
        },

        startHealthDeclarationTimer: function() {
            const self = this;
            self.timerCountdown = 120; // 2 minutes in seconds
            self.updateTimerDisplay();

            self.healthDeclarationTimer = setInterval(function() {
                self.timerCountdown--;
                self.updateTimerDisplay();

                if (self.timerCountdown <= 0) {
                    self.clearHealthDeclarationTimer();
                    self.hideHealthDeclarationForm();

                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'info',
                            title: 'Time Expired',
                            text: 'Health declaration window has closed',
                            timer: 2000,
                            showConfirmButton: false
                        });
                    }
                }
            }, 1000);
        },

        clearHealthDeclarationTimer: function() {
            if (this.healthDeclarationTimer) {
                clearInterval(this.healthDeclarationTimer);
                this.healthDeclarationTimer = null;
            }
        },

        updateTimerDisplay: function() {
            $('#timer-countdown').text(this.timerCountdown);
        },

        updateHealthStatus: function(attendanceId, healthStatus) {
            const self = this;
            $.ajax({
                url: '/Home/UpdateHealthStatus',
                method: 'POST',
                data: {
                    attendance_id: attendanceId,
                    health_status: healthStatus
                },
                success: function(response) {
                    if (response.success) {
                        console.log('Health status updated:', healthStatus);

                        if (healthStatus === 'FIT') {
                            HomePage.common.showSuccess('You are now marked as FIT and can enter', 'Status Updated');
                        }
                    } else {
                        HomePage.common.showError(response.message || 'Failed to update health status', 'Update Failed');
                    }
                },
                error: function() {
                    HomePage.common.showError('Failed to update health status');
                }
            });
        }
    },

    // HistoryLogs.cshtml - History logs with filtering and export
    historyLogs: {
        historyTable: null,

        init: function() {
            const self = this;

            // Initialize DataTable
            self.historyTable = $('#history-table').DataTable({
                ajax: {
                    url: '/Home/GetHistoryLogs',
                    dataSrc: function(data) {
                        if (data.success && data.data) {
                            return data.data;
                        }
                        return [];
                    }
                },
                columns: [
                    { data: 'attendance_id' },
                    { data: 'employee_id' },
                    {
                        data: 'time_in',
                        render: function(data) {
                            if (!data) return '';
                            return HomePage.common.formatDate(data);
                        }
                    },
                    {
                        data: 'time_out',
                        render: function(data) {
                            if (!data) return 'Active';
                            return HomePage.common.formatDate(data);
                        }
                    },
                    {
                        data: 'health_status',
                        render: function(data) {
                            if (data === 'FIT') {
                                return '<span class="badge bg-success">FIT</span>';
                            } else if (data === 'UNFIT') {
                                return '<span class="badge bg-danger">UNFIT</span>';
                            }
                            return data;
                        }
                    },
                    {
                        data: 'updated_by',
                        render: function(data) {
                            return data || '-';
                        }
                    },
                    {
                        data: 'updated_at',
                        render: function(data) {
                            if (!data) return '-';
                            return HomePage.common.formatDate(data);
                        }
                    }
                ],
                order: [[2, 'desc']], // Sort by Time In descending
                pageLength: 25,
                language: {
                    emptyTable: 'No attendance records available'
                }
            });

            // Show export button when data exists
            self.historyTable.on('draw', function() {
                const hasData = self.historyTable.data().length > 0;
                $('#export-button').toggle(hasData);
                $('#no-data-message').toggle(!hasData);
            });

            // Apply filters button
            $('#apply-filters').on('click', function() {
                self.applyFilters();
            });

            // Clear filters button
            $('#clear-filters').on('click', function() {
                self.clearFilters();
            });

            // Export to Excel button
            $('#export-button').on('click', function() {
                self.exportToExcel();
            });
        },

        applyFilters: function() {
            const employeeId = $('#filter-employee-id').val();
            const fromDate = $('#filter-from-date').val();
            const toDate = $('#filter-to-date').val();
            const healthStatus = $('#filter-health-status').val();

            // Reload DataTable with filters
            this.historyTable.ajax.url(this.buildFilterUrl(employeeId, fromDate, toDate, healthStatus)).load();
        },

        clearFilters: function() {
            $('#filter-employee-id').val('');
            $('#filter-from-date').val('');
            $('#filter-to-date').val('');
            $('#filter-health-status').val('');

            // Reload DataTable without filters
            this.historyTable.ajax.url('/Home/GetHistoryLogs').load();
        },

        buildFilterUrl: function(employeeId, fromDate, toDate, healthStatus) {
            let url = '/Home/GetHistoryLogs?';
            const params = [];

            if (employeeId) params.push('employee_id=' + encodeURIComponent(employeeId));
            if (fromDate) params.push('from_date=' + encodeURIComponent(fromDate));
            if (toDate) params.push('to_date=' + encodeURIComponent(toDate));
            if (healthStatus) params.push('health_status=' + encodeURIComponent(healthStatus));

            return url + params.join('&');
        },

        exportToExcel: function() {
            // Get current filtered data from DataTable
            const tableData = this.historyTable.rows().data().toArray();

            if (tableData.length === 0) {
                HomePage.common.showWarning('No data available to export', 'No Data');
                return;
            }

            // Transform data for export
            const exportData = tableData.map(row => ({
                'Attendance ID': row.attendance_id,
                'Employee ID': row.employee_id,
                'Time In': HomePage.common.formatDateTime(row.time_in),
                'Time Out': row.time_out ? HomePage.common.formatDateTime(row.time_out) : 'Active',
                'Health Status': row.health_status,
                'Updated By': row.updated_by || '-',
                'Updated At': row.updated_at ? HomePage.common.formatDateTime(row.updated_at) : '-'
            }));

            // Create Excel file
            const ws = XLSX.utils.json_to_sheet(exportData);
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, 'Attendance');

            // Generate filename with timestamp
            const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, '-').replace('T', '_');
            const filename = `AttendanceExport_${timestamp}.xlsx`;

            // Download file
            XLSX.writeFile(wb, filename);

            // Show success message
            HomePage.common.showSuccess(`Exported ${tableData.length} records to Excel`, 'Export Successful');
        }
    }
};

// HomePage - Organized JavaScript for Home Controller views
// Structure: index (Kiosk scanning), historyLogs (History logs with export)

const HomePage = {
    // Common utilities
    common: {
        showSuccess: function(message, title = 'Success', shouldRestoreFocus = false) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: title,
                    text: message,
                    timer: 2000,
                    showConfirmButton: false,
                    didClose: shouldRestoreFocus ? function() {
                        HomePage.index.restoreFocus();
                    } : undefined
                });
            }
        },

        showError: function(message, title = 'Error', shouldRestoreFocus = false) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'error',
                    title: title,
                    text: message,
                    timer: 3000,
                    showConfirmButton: false,
                    didClose: shouldRestoreFocus ? function() {
                        HomePage.index.restoreFocus();
                    } : undefined
                });
            }
        },

        showWarning: function(message, title = 'Warning', shouldRestoreFocus = false) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'warning',
                    title: title,
                    text: message,
                    timer: 3000,
                    showConfirmButton: false,
                    didClose: shouldRestoreFocus ? function() {
                        HomePage.index.restoreFocus();
                    } : undefined
                });
            }
        },

        showCriticalAlert: function(message, title = 'Critical Alert', shouldRestoreFocus = false) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'error',
                    title: title,
                    text: message,
                    width: '80%',  // XXL size for visibility
                    timer: 3000,  // Auto-dismiss after 3 seconds
                    showConfirmButton: false,  // No button required
                    customClass: {
                        popup: 'critical-alert-popup',
                        title: 'critical-alert-title',
                        content: 'critical-alert-content'
                    },
                    backdrop: `rgba(0, 0, 0, 0.7)`,
                    didClose: shouldRestoreFocus ? function() {
                        HomePage.index.restoreFocus();
                    } : undefined
                });
            }
        },

        showSuccessAlert: function(message, title = 'Success', shouldRestoreFocus = false) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: title,
                    text: message,
                    width: '80%',  // XXL size for visibility
                    timer: 2000,  // Auto-dismiss after 2 seconds
                    showConfirmButton: false,
                    customClass: {
                        popup: 'success-alert-popup',
                        title: 'success-alert-title',
                        content: 'success-alert-content'
                    },
                    backdrop: `rgba(0, 0, 0, 0.4)`,
                    didClose: shouldRestoreFocus ? function() {
                        HomePage.index.restoreFocus();
                    } : undefined
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
        },
    },

    // Index.cshtml - Kiosk scanning functionality
    index: {
        // Global variables for health declaration timer
        healthDeclarationTimer: null,
        timerCountdown: window.healthDeclarationWindowSeconds,  // Use server config or fallback

        // Global variable for scan-result timeout
        scanResultTimeout: null,

        // Helper function to restore focus to employee_id input
        restoreFocus: function() {
            setTimeout(function() {
                $('#employee_id').focus();
            }, 100);
        },

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
                const waiverConsent = $('input[name="waiver_consent"]:checked').val();

                if (selectedStatus === 'UNFIT') {
                    // Show blocking message for UNFIT
                    HomePage.common.showCriticalAlert(
                        'You are not allowed to enter company premises due to health status.',
                        'Not Allowed',
                        true
                    );
                    self.updateHealthStatus(attendanceId, 'UNFIT', waiverConsent);
                } else if (selectedStatus === 'FIT') {
                    // When changing TO FIT, check if waiver is NOT_UNDERSTOOD
                    if (waiverConsent === 'NOT_UNDERSTOOD') {
                        // Show alert that TIME OUT will still be triggered due to waiver
                        HomePage.common.showCriticalAlert(
                            'You are marked as FIT but do not understand the waiver.',
                            'You are not allowed to enter',
                            true
                        );
                    }
                    // Update health status to FIT
                    self.updateHealthStatus(attendanceId, 'FIT', waiverConsent);
                }
            });

            // Waiver consent radio button change handler
            $('input[name="waiver_consent"]').on('change', function() {
                const selectedConsent = $(this).val();
                const attendanceId = $('#hd_attendance_id').val();
                const currentHealthStatus = $('input[name="health_status"]:checked').val();

                if (selectedConsent === 'NOT_UNDERSTOOD') {
                    // Show CRITICAL blocking alert for NOT_UNDERSTOOD (same as UNFIT styling)
                    HomePage.common.showCriticalAlert(
                        'You are marked as FIT but do not understand the waiver.',
                        'You are not allowed to enter',
                        true
                    );

                    // Update to NOT_UNDERSTOOD consent (this will auto-set TIME_OUT)
                    self.updateHealthStatus(attendanceId, currentHealthStatus, 'NOT_UNDERSTOOD');
                } else if (selectedConsent === 'UNDERSTOOD') {
                    // Update to UNDERSTOOD consent (will remove TIME_OUT if within window)
                    self.updateHealthStatus(attendanceId, currentHealthStatus, 'UNDERSTOOD');
                }
            });
        },

        processScan: function() {
            const self = this;
            const employeeId = $('#employee_id').val().trim();

            // Debug logging to track scan processing
            console.log('processScan called - employee_id:', employeeId);

            if (!employeeId) {
                console.error('Employee ID is empty - scan aborted');
                return;
            }

            // Clear the input at submission so characters from a scan that starts
            // while this request is in flight are not wiped by the response handler
            $('#employee_id').val('');

            // Clear any existing health declaration timer
            self.clearHealthDeclarationTimer();

            // Clear any existing scan-result message
            self.clearScanResult();

            // Hide health declaration form from previous scan (if any)
            self.hideHealthDeclarationForm();

            $.ajax({
                url: '/Home/Scan',
                method: 'POST',
                data: { employee_id: employeeId },
                success: function(response) {
                    console.log('Scan response:', response);
                    if (response.success) {
                        self.displayContractorInfo(response.data.contractor_info);
                        self.displayScanResult(response.message);

                        // Only show health declaration form for TIME IN (not TIME OUT)
                        if (response.message.includes("TIME OUT")) {
                            // TIME OUT operation - don't show health declaration form
                            self.hideHealthDeclarationForm();
                        } else {
                            // TIME IN operation - show health declaration form
                            self.showHealthDeclarationForm(response.data.attendance_id);
                        }

                        
                    } else {
                        self.displayScanError(response.message);
                    }

                    // Re-focus for next scan (input is cleared at submission time so
                    // in-flight scan characters are preserved)
                    $('#employee_id').focus();
                },
                error: function() {
                    HomePage.common.showError('Failed to process scan', 'Error', true);
                }
            });
        },

        displayContractorInfo: function(contractor) {
            if (!contractor) {
                $('#contractor-info').hide();
                return;
            }

            // Names only -- no provider_code / project codes in the kiosk block.
            $('#employee_name').html('<i class="fa-solid fa-user"></i> ' + (contractor.name || ''));
            $('#provider').html('<i class="fa-solid fa-building-user"></i> ' + (contractor.provider_name || ''));

            // Clear rows appended by a previous scan (back-to-back scans call this
            // repeatedly; TIME OUT scans render this block too).
            $('#project-rows').empty();

            // Per-project rows arrive as a JSON array string
            // [{"project_name":"...","area":"...","position":"..."}] ordered by project
            // code (index-aligned, unlike the DISTINCT areas CSV).
            var rows = [];
            try {
                rows = JSON.parse(contractor.project_rows || '[]') || [];
            } catch (e) {
                rows = []; // malformed payload -> row 1 only, never block the form
            }

            // Headers only make sense above project rows -- zero-project TIME OUT
            // scans keep the row-1-only look.
            $('#project-header').toggle(rows.length > 0);

            for (var i = 0; i < rows.length; i++) {
                var r = rows[i];
                $('#project-rows').append(
                    '<tr>' +
                    '<td>' + (r.project_name || '') + '</td>' +
                    '<td>' + (r.area || '') + '</td>' +
                    '<td>' + (r.position || '&mdash;') + '</td>' +
                    '</tr>'
                );
            }

            $('#contractor-info').show();
        },

        displayScanResult: function(message) {
            const self = this;

            // Clear any existing timeout first
            if (self.scanResultTimeout) {
                clearTimeout(self.scanResultTimeout);
            }

            // Display success message inline under employee_id field
            if (message.includes('TIME OUT')) {
                // Use blue/primary styling for TIME OUT messages
                $('#scan-result').html('<div class="time-out-success-popup mt-2" role="alert">' + message + '</div>');
            } else {
                // Use green styling for other success messages
                $('#scan-result').html('<div class="alert alert-success mt-2" role="alert">' + message + '</div>');
            }

            // Auto-clear the message after 5 seconds
            self.scanResultTimeout = setTimeout(function() {
                $('#scan-result').fadeOut('slow', function() {
                    $(this).empty().show();
                    self.scanResultTimeout = null;
                });
            }, 5000);
        },

        clearScanResult: function() {
            const self = this;

            // Clear the timeout if exists
            if (self.scanResultTimeout) {
                clearTimeout(self.scanResultTimeout);
                self.scanResultTimeout = null;
            }

            // Clear and hide the scan-result div immediately
            $('#scan-result').stop(true, true).fadeOut(0, function() {
                $(this).empty().show();
            });
        },

        displayScanError: function(message) {
            const self = this;

            // Clear any existing timeout first
            if (self.scanResultTimeout) {
                clearTimeout(self.scanResultTimeout);
            }

            // Display error message inline under employee_id field
            $('#scan-result').html('<div class="alert alert-danger mt-2" role="alert">' + message + '</div>');

            // Auto-clear the error message after 5 seconds
            self.scanResultTimeout = setTimeout(function() {
                $('#scan-result').fadeOut('slow', function() {
                    $(this).empty().show();
                    self.scanResultTimeout = null;
                });
            }, 7000);

            // Clear contractor info on error
            $('#contractor-info').hide();
        },

        showHealthDeclarationForm: function(attendanceId) {
            $('#hd_attendance_id').val(attendanceId);
            $('#health-declaration-section').show();
            $('#no-declaration-message').hide();
            $('#home-logo').hide();

            // Reset form to default FIT status
            $('#fit').prop('checked', true);  // Reset health status to FIT

            // Reset waiver consent to default "I understand"
            $('#understood').prop('checked', true);  // Reset waiver consent to UNDERSTOOD

            // Start health declaration timer (server-configured seconds)
            this.startHealthDeclarationTimer();
        },

        hideHealthDeclarationForm: function() {
            $('#health-declaration-section').hide();
            $('#no-declaration-message').show();
            $('#home-logo').show();
            $('#hd_attendance_id').val('');
            this.clearHealthDeclarationTimer();
        },

        startHealthDeclarationTimer: function() {
            const self = this;
            self.timerCountdown = window.healthDeclarationWindowSeconds || 120;  // Use server config or fallback
            self.updateTimerDisplay();

            self.healthDeclarationTimer = setInterval(function() {
                self.timerCountdown--;
                self.updateTimerDisplay();

                if (self.timerCountdown <= 0) {
                    self.clearHealthDeclarationTimer();
                    self.hideHealthDeclarationForm();
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

        updateHealthStatus: function(attendanceId, healthStatus, waiverConsent) {
            const self = this;
            $.ajax({
                url: '/Home/UpdateHealthStatus',
                method: 'POST',
                data: {
                    attendance_id: attendanceId,
                    health_status: healthStatus,
                    waiver_consent: waiverConsent || 'UNDERSTOOD'  // Default if not provided
                },
                success: function(response) {
                    if (response.success) {
                        console.log('Health status and waiver consent updated:', healthStatus, waiverConsent);

                        if (healthStatus === 'FIT' && waiverConsent === 'UNDERSTOOD') {
                            // Success: FIT + UNDERSTOOD = allowed to enter
                            HomePage.common.showSuccessAlert('FIT and understand the waiver.', 'ALLOWED TO ENTER', true);
                        } else if (healthStatus === 'FIT' && waiverConsent === 'NOT_UNDERSTOOD') {
                            // Error: FIT but NOT_UNDERSTOOD = not allowed (TIME_OUT set)
                            // No need to show alert here since it's already shown in the change handler
                            //console.log('TIME OUT set due to NOT_UNDERSTOOD waiver consent');
                            HomePage.common.showCriticalAlert('You do not understand the waiver.', 'NOT ALLOWED TO ENTER', true);
                        } else if (healthStatus === 'UNFIT' && waiverConsent === 'UNDERSTOOD') {
                            HomePage.common.showCriticalAlert('You are UNFIT.', 'NOT ALLOWED TO ENTER', true);
                        }
                    } else {
                        HomePage.common.showError(response.message || 'Failed to update health status and waiver consent', 'Update Failed', true);
                    }
                },
                error: function() {
                    HomePage.common.showError('Failed to update health status and waiver consent', 'Error', true);
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
                    { data: 'employee_id' },
                    { data: 'name' },
                    { data: 'provider_code' },
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
                            if (!data) return '';
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
                        data: 'waiver_consent',
                        render: function(data) {
                            if (data === 'UNDERSTOOD') {
                                return '<span class="badge bg-success">Understood</span>';
                            } else if (data === 'NOT_UNDERSTOOD') {
                                return '<span class="badge bg-warning">Not Understood</span>';
                            }
                            return data || '-';
                        }
                    }
                ],
                searching: true,  // Enable global search
                order: [[3, 'desc']], // Sort by Time In descending (column index 3)
                pageLength: 10,
                language: {
                    emptyTable: 'No attendance records available',
                    search: 'Search:'  // Label for global search box
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
            const fromDate = $('#filter-from-date').val();
            const toDate = $('#filter-to-date').val();
            const healthStatus = $('#filter-health-status').val();

            // Reload DataTable with filters
            this.historyTable.ajax.url(this.buildFilterUrl(fromDate, toDate, healthStatus)).load();
        },

        clearFilters: function() {
            $('#filter-from-date').val('');
            $('#filter-to-date').val('');
            $('#filter-health-status').val('');

            // Reload DataTable without filters
            this.historyTable.ajax.url('/Home/GetHistoryLogs').load();
        },

        buildFilterUrl: function(fromDate, toDate, healthStatus) {
            let url = '/Home/GetHistoryLogs?';
            const params = [];

            if (fromDate) params.push('from_date=' + encodeURIComponent(fromDate));
            if (toDate) params.push('to_date=' + encodeURIComponent(toDate));
            if (healthStatus) params.push('health_status=' + encodeURIComponent(healthStatus));

            return url + params.join('&');
        },

        exportToExcel: function() {
            // Get current filtered data from DataTable (respecting search and filters)
            const tableData = this.historyTable.rows({ search: 'applied' }).data().toArray();

            if (tableData.length === 0) {
                HomePage.common.showWarning('No data available to export', 'No Data');
                return;
            }

            // Transform data for export
            const exportData = tableData.map(row => ({
                'Employee ID': row.employee_id,
                'Employee Name': row.name,
                'Provider': row.provider_code,
                'Time In': HomePage.common.formatDateTime(row.time_in),
                'Time Out': row.time_out ? HomePage.common.formatDateTime(row.time_out) : 'Active',
                'Health Status': row.health_status,
                'Waiver Consent': row.waiver_consent === 'UNDERSTOOD' ? 'Understood' : row.waiver_consent === 'NOT_UNDERSTOOD' ? 'Not Understood' : row.waiver_consent || '-'
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

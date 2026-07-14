// AttendancePage - Real-time attendance monitoring with SignalR
// This is the most complex view with 400+ lines including SignalR, DataTables, SweetAlert2

const AttendancePage = {
    // SignalR connection
    signalR: {
        connection: null,

        init: function() {
            const self = this;

            // Create SignalR connection
            self.connection = new signalR.HubConnectionBuilder()
                .withUrl("/attendanceHub")
                .build();

            // Handle real-time attendance updates
            self.connection.on("ReceiveAttendanceUpdate", function(data) {
                AttendancePage.index.updateTable(data);
            });

            // Handle waiver approval notifications
            self.connection.on("WaiverApproved", function(attendanceId) {
                AttendancePage.index.clearRowBackground(attendanceId);

                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'success',
                        title: 'Waiver Approved',
                        text: 'Contractor allowed to enter',
                        timer: 2000,
                        showConfirmButton: false
                    });
                }
            });

            // Start the connection
            self.connection.start().catch(function(err) {
                console.error("SignalR connection error:", err);

                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'error',
                        title: 'Connection Error',
                        text: 'Real-time updates not available'
                    });
                }
            });
        }
    },

    // Index.cshtml - Main attendance monitoring functionality
    index: {
        table: null,
        healthDeclarationTimer: null,
        timerCountdown: 120,

        init: function() {
            const self = this;

            // Initialize SignalR
            AttendancePage.signalR.init();

            // Initialize DataTable
            self.table = $('#attendance-table').DataTable({
                ajax: {
                    url: '/Attendance/GetTodayAttendance',
                    dataSrc: 'data'
                },
                columns: [
                    {
                        data: 'employee_id',
                        render: function(data) {
                            if (!data) return '';
                            // Convert each character to a bullet symbol
                            return '•'.repeat(data.length);
                        }
                    },
                    { data: 'name' },
                    { data: 'provider_code' },
                    {
                        data: 'time_in',
                        render: function(data) {
                            if (!data) return '';
                            return new Date(data).toLocaleTimeString();
                        }
                    },
                    {
                        data: 'time_out',
                        render: function(data) {
                            if (!data) return '<span class="badge bg-success">Active</span>';
                            return new Date(data).toLocaleTimeString();
                        }
                    },
                    {
                        data: 'health_status',
                        render: function(data, type, row) {
                            if (data === 'FIT') {
                                return '<span class="badge bg-success">FIT</span>';
                            } else if (data === 'UNFIT') {
                                return '<span class="badge bg-danger">UNFIT</span>';
                            } else if (data === 'Waived') {
                                return '<span class="badge bg-warning">Waived</span>';
                            }
                            return data;
                        }
                    },
                    {
                        data: null,
                        render: function(data, type, row) {
                            let actions = '';

                            // Show Approve Waiver button only for UNFIT status
                            if (row.health_status === 'UNFIT') {
                                actions += `<button class="btn btn-sm btn-approve-waiver" data-attendance-id="${data.attendance_id}">Approve Waiver</button> `;
                            }

                            // Show Time Out button only for active records
                            if (!data.time_out && data.health_status !== 'UNFIT') {
                                actions += `<button class="btn btn-sm btn-time-out" data-attendance-id="${data.attendance_id}">Time Out</button>`;
                            }

                            return actions;
                        }
                    }
                ],
                order: [[3, 'desc']], // Sort by Time In descending
                pageLength: 25,
                language: {
                    emptyTable: 'No attendance records for today'
                }
            });

            // Set focus on employee_id input
            $('#employee_id').focus();

            // ID Scanning - Enter key processing
            $('#employee_id').on('keypress', function(e) {
                if (e.which === 13) { // Enter key
                    e.preventDefault();
                    self.processScan();
                }
            });

            // Health declaration form submission
            $('#health-declaration-form').on('submit', function(e) {
                e.preventDefault();
                self.submitHealthDeclaration();
            });

            // Medicine radio button change handler
            $('input[name="medicine"]').on('change', function() {
                if ($(this).val() === 'Yes') {
                    $('#medicine-names-group').show();
                } else {
                    $('#medicine-names-group').hide();
                    $('#medicine_names').val('');
                }
            });

            // Approve waiver button (delegated event for dynamic rows)
            $(document).on('click', '.btn-approve-waiver', function() {
                const attendanceId = $(this).data('attendance-id');
                self.approveWaiver(attendanceId);
            });

            // Time out button (delegated event for dynamic rows)
            $(document).on('click', '.btn-time-out', function() {
                const attendanceId = $(this).data('attendance-id');
                self.processTimeOut(attendanceId);
            });
        },

        processScan: function() {
            const self = this;
            const employeeId = $('#employee_id').val().trim();
            if (!employeeId) return;

            // Clear any existing health declaration timer
            self.clearHealthDeclarationTimer();

            $.ajax({
                url: '/Attendance/Scan',
                method: 'POST',
                data: { employee_id: employeeId },
                success: function(response) {
                    if (response.success) {
                        self.displayContractorInfo(response.data.contractor_info);
                        self.displayScanResult(response.message);

                        // Show health declaration form if needed
                        if (response.data.requires_health_declaration) {
                            self.showHealthDeclarationForm(response.data.attendance_id);
                        }

                        // Clear input for next scan
                        $('#employee_id').val('');
                        $('#employee_id').focus();
                    } else {
                        self.displayScanError(response.message);
                    }
                },
                error: function() {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'error',
                            title: 'Error',
                            text: 'Failed to process scan'
                        });
                    }
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
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: 'Scan Successful',
                    text: message,
                    timer: 2000,
                    showConfirmButton: false
                });
            }
        },

        displayScanError: function(message) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'error',
                    title: 'Scan Failed',
                    text: message,
                    timer: 3000,
                    showConfirmButton: false
                });
            }

            // Clear contractor info on error
            $('#contractor-info').hide();
        },

        showHealthDeclarationForm: function(attendanceId) {
            const self = this;
            $('#hd_attendance_id').val(attendanceId);
            $('#health-declaration-section').show();

            // Reset form
            $('#health-declaration-form')[0].reset();
            $('input[name="medicine"][value="No"]').prop('checked', true);
            $('#medicine-names-group').hide();

            // Start 2-minute timer
            self.startHealthDeclarationTimer();
        },

        hideHealthDeclarationForm: function() {
            $('#health-declaration-section').hide();
            $('#hd_attendance_id').val('');
            this.clearHealthDeclarationTimer();
        },

        startHealthDeclarationTimer: function() {
            const self = this;
            self.timerCountdown = 120; // 2 minutes in seconds

            self.healthDeclarationTimer = setInterval(function() {
                self.timerCountdown--;

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

        submitHealthDeclaration: function() {
            const self = this;

            // Gather symptoms
            const symptoms = [];
            $('input[type="checkbox"]:checked').each(function() {
                symptoms.push($(this).val());
            });

            const healthDeclaration = {
                attendance_id: $('#hd_attendance_id').val(),
                symptoms: symptoms.join(','),
                illness: $('#illness').val(),
                medicine: $('input[name="medicine"]:checked').val(),
                medicine_names: $('#medicine_names').val()
            };

            $.ajax({
                url: '/Attendance/SubmitHealthDeclaration',
                method: 'POST',
                data: healthDeclaration,
                success: function(response) {
                    if (response.success) {
                        self.hideHealthDeclarationForm();

                        if (typeof Swal !== 'undefined') {
                            Swal.fire({
                                icon: 'success',
                                title: 'Health Declaration Submitted',
                                text: response.message,
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                    } else {
                        if (typeof Swal !== 'undefined') {
                            Swal.fire({
                                icon: 'error',
                                title: 'Submission Failed',
                                text: response.message
                            });
                        }
                    }
                },
                error: function() {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'error',
                            title: 'Error',
                            text: 'Failed to submit health declaration'
                        });
                    }
                }
            });
        },

        approveWaiver: function(attendanceId) {
            const self = this;

            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    title: 'Approve Waiver?',
                    text: 'Allow this contractor to enter despite health declaration?',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#3085d6',
                    confirmButtonText: 'Yes, approve'
                }).then((result) => {
                    if (result.isConfirmed) {
                        $.ajax({
                            url: '/Attendance/ApproveWaiver',
                            method: 'POST',
                            data: { attendance_id: attendanceId },
                            success: function(response) {
                                if (response.success) {
                                    Swal.fire('Approved', 'Waiver approved successfully', 'success');
                                    self.highlightRow(attendanceId, 'warning');
                                } else {
                                    Swal.fire('Error', response.message, 'error');
                                }
                            }
                        });
                    }
                });
            }
        },

        processTimeOut: function(attendanceId) {
            const self = this;

            $.ajax({
                url: '/Attendance/TimeOut',
                method: 'POST',
                data: { attendance_id: attendanceId },
                success: function(response) {
                    if (response.success) {
                        if (typeof Swal !== 'undefined') {
                            Swal.fire({
                                icon: 'success',
                                title: 'Time Out Recorded',
                                text: response.message,
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                    } else {
                        if (typeof Swal !== 'undefined') {
                            Swal.fire({
                                icon: 'error',
                                title: 'Failed',
                                text: response.message
                            });
                        }
                    }
                }
            });
        },

        updateTable: function(data) {
            if (!this.table) return;

            // Reload table data
            this.table.ajax.reload(null, false); // null = callback, false = no reset paging

            // Highlight the updated row
            this.highlightRow(data.attendance_id, 'info');
        },

        highlightRow: function(attendanceId, color) {
            if (!this.table) return;

            // Find and highlight the row
            this.table.rows().every(function() {
                const row = this.node();
                const rowData = this.data();

                if (rowData.attendance_id == attendanceId) {
                    $(row).addClass('table-' + color);

                    // Remove highlight after 3 seconds
                    setTimeout(function() {
                        $(row).removeClass('table-' + color);
                    }, 3000);
                }
            });
        },

        clearRowBackground: function(attendanceId) {
            if (!this.table) return;

            this.table.rows().every(function() {
                const row = this.node();
                const rowData = this.data();

                if (rowData.attendance_id == attendanceId) {
                    $(row).removeClass('table-warning');
                }
            });
        }
    }
};

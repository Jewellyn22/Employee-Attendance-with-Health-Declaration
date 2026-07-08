// AdminPage - Organized JavaScript for Admin Controller views
// Structure: providers, projects, contractors, systemConfig, timeLogs, index

const AdminPage = {
    // Common utilities shared across all admin views
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

        showConfirmation: function(message, onConfirm) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    title: 'Are you sure?',
                    text: message,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#d33',
                    confirmButtonText: 'Yes, proceed'
                }).then((result) => {
                    if (result.isConfirmed && onConfirm) {
                        onConfirm();
                    }
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

    // Providers.cshtml - Providers CRUD
    providers: {
        providerTable: null,

        init: function() {
            const self = this;

            // Initialize DataTable
            self.providerTable = $('#providers-table').DataTable({
                ajax: {
                    url: '/Admin/GetAllProviders',
                    dataSrc: function(data) {
                        return data.success ? data.data : [];
                    }
                },
                columns: [
                    { data: 'provider_code' },
                    { data: 'provider_name' },
                    { data: 'provider_pic' },
                    { data: 'provider_pic_number' },
                    {
                        data: 'active',
                        render: function(data) {
                            return data === 1 ? 'Active' : 'Inactive';
                        }
                    },
                    {
                        data: null,
                        render: function(data) {
                            return `
                                <button class="btn btn-sm btn-edit" data-provider-code="${data.provider_code}">Edit</button>
                                <button class="btn btn-sm btn-set-inactive" data-provider-code="${data.provider_code}">Set Inactive</button>
                            `;
                        }
                    }
                ],
                pageLength: 25
            });

            // Add provider button
            $('#add-provider').on('click', function() {
                $('#provider-form')[0].reset();
                $('#provider_code').val('');
                $('#provider-modal .modal-title').text('Add New Provider');
                $('#provider-modal').modal('show');
            });

            // Edit provider button
            $(document).on('click', '.btn-edit', function() {
                const providerCode = $(this).data('provider-code');
                const provider = self.providerTable.row($(this).closest('tr')).data();

                $('#provider_code').val(provider.provider_code);
                $('#provider_name').val(provider.provider_name);
                $('#provider_address').val(provider.provider_address);
                $('#provider_pic').val(provider.provider_pic);
                $('#provider_pic_number').val(provider.provider_pic_number);

                $('#provider-modal .modal-title').text('Edit Provider');
                $('#provider-modal').modal('show');
            });

            // Set inactive button
            $(document).on('click', '.btn-set-inactive', function() {
                const providerCode = $(this).data('provider-code');
                const provider = self.providerTable.row($(this).closest('tr')).data();

                AdminPage.common.showConfirmation(
                    `Set ${provider.provider_name} to inactive status?`,
                    function() {
                        provider.active = 0;
                        $.ajax({
                            url: '/Admin/SetProviderInactive',
                            method: 'POST',
                            data: provider,
                            success: function(response) {
                                if (response.success) {
                                    AdminPage.common.showSuccess('Provider set to inactive');
                                    self.providerTable.ajax.reload();
                                }
                            }
                        });
                    }
                );
            });

            // Save provider
            $('#save-provider').on('click', function() {
                const provider = {
                    provider_code: $('#provider_code').val() || 'PRV-' + Date.now(),
                    provider_name: $('#provider_name').val(),
                    provider_address: $('#provider_address').val(),
                    provider_pic: $('#provider_pic').val(),
                    provider_pic_number: $('#provider_pic_number').val(),
                    active: 1
                };

                const isNewProvider = !$('#provider_code').val();
                const url = isNewProvider ? '/Admin/CreateProvider' : '/Admin/UpdateProvider';

                $.ajax({
                    url: url,
                    method: 'POST',
                    data: provider,
                    success: function(response) {
                        if (response.success) {
                            $('#provider-modal').modal('hide');
                            AdminPage.common.showSuccess(response.message);
                            self.providerTable.ajax.reload();
                        } else {
                            AdminPage.common.showError(response.message);
                        }
                    }
                });
            });
        }
    },

    // Projects.cshtml - Projects CRUD
    projects: {
        projectTable: null,

        init: function() {
            const self = this;

            // Initialize DataTable
            self.projectTable = $('#projects-table').DataTable({
                ajax: {
                    url: '/Admin/GetAllProjects',
                    dataSrc: function(data) {
                        return data.success ? data.data : [];
                    }
                },
                columns: [
                    { data: 'project_code' },
                    { data: 'project_name' },
                    { data: 'provider_code' },
                    { data: 'project_address' },
                    { data: 'project_pic' },
                    { data: 'project_pic_number' },
                    {
                        data: 'active',
                        render: function(data) {
                            return data === 1 ? 'Active' : 'Inactive';
                        }
                    },
                    {
                        data: null,
                        render: function(data) {
                            return `
                                <button class="btn btn-sm btn-edit" data-project-code="${data.project_code}">Edit</button>
                                <button class="btn btn-sm btn-delete" data-project-code="${data.project_code}">Delete</button>
                            `;
                        }
                    }
                ],
                pageLength: 25
            });

            // Load providers for dropdown
            self.loadProviders();

            // Add project button
            $('#add-project').on('click', function() {
                $('#project-form')[0].reset();
                $('#project_code').val('');
                $('#project-modal .modal-title').text('Add New Project');
                $('#project-modal').modal('show');
            });

            // Edit project button
            $(document).on('click', '.btn-edit', function() {
                const projectCode = $(this).data('project-code');
                const project = self.projectTable.row($(this).closest('tr')).data();

                $('#project_code').val(project.project_code);
                $('#project_name').val(project.project_name);
                $('#project_provider_code').val(project.provider_code);
                $('#project_address').val(project.project_address);
                $('#project_pic').val(project.project_pic);
                $('#project_pic_number').val(project.project_pic_number);

                $('#project-modal .modal-title').text('Edit Project');
                $('#project-modal').modal('show');
            });

            // Delete project button
            $(document).on('click', '.btn-delete', function() {
                const projectCode = $(this).data('project-code');
                const project = self.projectTable.row($(this).closest('tr')).data();

                AdminPage.common.showConfirmation(
                    `Delete ${project.project_name}?`,
                    function() {
                        $.ajax({
                            url: '/Admin/DeleteProject',
                            method: 'POST',
                            data: { project_code: projectCode },
                            success: function(response) {
                                if (response.success) {
                                    AdminPage.common.showSuccess('Project deleted');
                                    self.projectTable.ajax.reload();
                                }
                            }
                        });
                    }
                );
            });

            // Save project
            $('#save-project').on('click', function() {
                const project = {
                    project_code: $('#project_code').val() || 'PRJ-' + Date.now(),
                    project_name: $('#project_name').val(),
                    provider_code: $('#project_provider_code').val(),
                    project_address: $('#project_address').val(),
                    project_pic: $('#project_pic').val(),
                    project_pic_number: $('#project_pic_number').val(),
                    active: 1
                };

                const isNewProject = !$('#project_code').val();
                const url = isNewProject ? '/Admin/CreateProject' : '/Admin/UpdateProject';

                $.ajax({
                    url: url,
                    method: 'POST',
                    data: project,
                    success: function(response) {
                        if (response.success) {
                            $('#project-modal').modal('hide');
                            AdminPage.common.showSuccess(response.message);
                            self.projectTable.ajax.reload();
                        } else {
                            AdminPage.common.showError(response.message);
                        }
                    }
                });
            });
        },

        loadProviders: function() {
            $.ajax({
                url: '/Admin/GetAllProviders',
                method: 'GET',
                success: function(data) {
                    if (data.success && data.data) {
                        const dropdown = $('#project_provider_code');
                        dropdown.empty();
                        dropdown.append('<option value="">Select Provider</option>');
                        data.data.forEach(function(provider) {
                            dropdown.append(`<option value="${provider.provider_code}">${provider.provider_name}</option>`);
                        });
                    }
                }
            });
        }
    },

    // Contractors.cshtml - Contractors CRUD
    contractors: {
        contractorTable: null,

        init: function() {
            const self = this;

            // Initialize Select2 for dropdowns
            $('#provider_code').select2({
                placeholder: 'Select Provider',
                allowClear: true,
                width: '100%'
            });

            $('#project_code').select2({
                placeholder: 'Select Project',
                allowClear: true,
                width: '100%'
            });

            // Initialize DataTable
            self.contractorTable = $('#contractors-table').DataTable({
                ajax: {
                    url: '/Admin/GetAllContractors',
                    dataSrc: function(data) {
                        return data.success ? data.data : [];
                    }
                },
                columns: [
                    { data: 'employee_id' },
                    { data: 'name' },
                    {
                        data: 'provider_code',
                        render: function(data, type, row) {
                            return row.provider_name ? `${row.provider_name} (${data})` : data;
                        }
                    },
                    {
                        data: 'project_code',
                        render: function(data, type, row) {
                            return row.project_name ? `${row.project_name} (${data})` : data;
                        }
                    },
                    { data: 'position' },
                    { data: 'area_of_destination' },
                    {
                        data: 'active',
                        render: function(data) {
                            return data === 1 ? 'Active' : 'Inactive';
                        }
                    },
                    {
                        data: null,
                        render: function(data) {
                            return `
                                <button class="btn btn-sm btn-edit" data-employee-id="${data.employee_id}">Edit</button>
                                <button class="btn btn-sm btn-set-inactive" data-employee-id="${data.employee_id}">Set Inactive</button>
                            `;
                        }
                    }
                ],
                pageLength: 25
            });

            // Load providers and projects for dropdowns
            self.loadProviders();
            self.loadProjects();

            // Add contractor button
            $('#add-contractor').on('click', function() {
                $('#contractor-form')[0].reset();
                $('#employee_id').val('');
                $('#provider_code').val(null).trigger('change');
                $('#project_code').val(null).trigger('change');
                $('#contractor-modal .modal-title').text('Add New Contractor');
                $('#contractor-modal').modal('show');
            });

            // Edit contractor button
            $(document).on('click', '.btn-edit', function() {
                const employeeId = $(this).data('employee-id');
                const contractor = self.contractorTable.row($(this).closest('tr')).data();

                $('#employee_id').val(contractor.employee_id);
                $('#name').val(contractor.name);
                $('#age').val(contractor.age);
                $('#gender').val(contractor.gender);
                $('#birthdate').val(self.formatDateForInput(contractor.birthdate));
                $('#contact_number').val(contractor.contact_number);
                $('#area_of_destination').val(contractor.area_of_destination);
                $('#provider_code').val(contractor.provider_code).trigger('change');
                $('#project_code').val(contractor.project_code).trigger('change');
                $('#position').val(contractor.position);

                $('#contractor-modal .modal-title').text('Edit Contractor');
                $('#contractor-modal').modal('show');
            });

            // Set inactive button
            $(document).on('click', '.btn-set-inactive', function() {
                const employeeId = $(this).data('employee-id');
                const contractor = self.contractorTable.row($(this).closest('tr')).data();

                AdminPage.common.showConfirmation(
                    `Set ${contractor.name} (${employeeId}) to inactive status?`,
                    function() {
                        contractor.active = 0;
                        $.ajax({
                            url: '/Admin/SetContractorInactive',
                            method: 'POST',
                            data: contractor,
                            success: function(response) {
                                if (response.success) {
                                    AdminPage.common.showSuccess('Contractor set to inactive');
                                    self.contractorTable.ajax.reload();
                                } else {
                                    AdminPage.common.showError(response.message);
                                }
                            }
                        });
                    }
                );
            });

            // Save contractor
            $('#save-contractor').on('click', function() {
                const contractor = {
                    employee_id: $('#employee_id').val(),
                    name: $('#name').val(),
                    age: $('#age').val() ? parseInt($('#age').val()) : 0,
                    gender: $('#gender').val(),
                    birthdate: $('#birthdate').val(),
                    contact_number: $('#contact_number').val(),
                    address: '',
                    area_of_destination: $('#area_of_destination').val(),
                    provider_code: $('#provider_code').val(),
                    project_code: $('#project_code').val(),
                    position: $('#position').val(),
                    active: 1
                };

                const isNewContractor = !$('#employee_id').val() || $('#employee_id').val() === '';
                const url = isNewContractor ? '/Admin/CreateContractor' : '/Admin/UpdateContractor';

                $.ajax({
                    url: url,
                    method: 'POST',
                    data: contractor,
                    success: function(response) {
                        if (response.success) {
                            $('#contractor-modal').modal('hide');
                            AdminPage.common.showSuccess(response.message);
                            self.contractorTable.ajax.reload();
                        } else {
                            AdminPage.common.showError(response.message);
                        }
                    },
                    error: function() {
                        AdminPage.common.showError('Failed to save contractor');
                    }
                });
            });

            // Update projects when provider changes
            $('#provider_code').on('change', function() {
                const providerCode = $(this).val();
                self.loadProjects(providerCode);
            });
        },

        loadProviders: function() {
            $.ajax({
                url: '/Admin/GetAllProviders',
                method: 'GET',
                success: function(response) {
                    if (response.success && response.data) {
                        response.data.forEach(provider => {
                            $('#provider_code').append('<option value="' + provider.provider_code + '">' +
                                provider.provider_name + ' (' + provider.provider_code + ')</option>');
                        });
                    }
                }
            });
        },

        loadProjects: function(providerCode = null) {
            $('#project_code').empty().append('<option value="">Select Project</option>');

            $.ajax({
                url: '/Admin/GetAllProjects',
                method: 'GET',
                success: function(response) {
                    if (response.success && response.data) {
                        response.data.forEach(project => {
                            // Filter by provider if specified
                            if (!providerCode || project.provider_code === providerCode) {
                                $('#project_code').append('<option value="' + project.project_code + '">' +
                                    project.project_name + ' (' + project.project_code + ')</option>');
                            }
                        });
                    }
                }
            });
        },

        formatDateForInput: function(dateString) {
            if (!dateString) return '';
            const date = new Date(dateString);
            return date.toISOString().split('T')[0];
        }
    },

    // SystemConfig.cshtml - System configuration management
    systemConfig: {
        configTable: null,

        init: function() {
            const self = this;

            // Initialize DataTable
            self.configTable = $('#config-table').DataTable({
                ajax: {
                    url: '/Admin/GetAllSystemConfigs',
                    dataSrc: function(data) {
                        return data.success ? data.data : [];
                    }
                },
                columns: [
                    { data: 'key' },
                    { data: 'value' },
                    { data: 'description' },
                    {
                        data: null,
                        render: function(data) {
                            return `
                                <button class="btn btn-sm btn-edit" data-config-key="${data.key}">Edit</button>
                            `;
                        }
                    }
                ],
                pageLength: 25
            });

            // Edit config button
            $(document).on('click', '.btn-edit', function() {
                const configKey = $(this).data('config-key');
                const config = self.configTable.row($(this).closest('tr')).data();

                $('#config_key').val(config.key);
                $('#config_value').val(config.value);
                $('#config_description').val(config.description);

                // Show guidance for specific configs
                self.showConfigGuidance(config.key);

                $('#config-modal').modal('show');
            });

            // Save config
            $('#save-config').on('click', function() {
                const config = {
                    key: $('#config_key').val(),
                    value: $('#config_value').val(),
                    description: $('#config_description').val()
                };

                // Validate numeric configs
                if (config.key === 'DebounceThresholdMinutes' || config.key === 'HealthDeclarationWindowMinutes') {
                    const numValue = parseFloat(config.value);
                    if (isNaN(numValue) || numValue < 0.1 || numValue > 60) {
                        AdminPage.common.showError('Value must be between 0.1 and 60 minutes', 'Validation Error');
                        return;
                    }
                }

                $.ajax({
                    url: '/Admin/UpdateSystemConfig',
                    method: 'POST',
                    data: config,
                    success: function(response) {
                        if (response.success) {
                            $('#config-modal').modal('hide');
                            AdminPage.common.showSuccess('Configuration updated successfully');
                            self.configTable.ajax.reload();
                        } else {
                            AdminPage.common.showError(response.message);
                        }
                    },
                    error: function() {
                        AdminPage.common.showError('Failed to update configuration');
                    }
                });
            });
        },

        showConfigGuidance: function(configKey) {
            let guidance = '';

            switch(configKey) {
                case 'DebounceThresholdMinutes':
                    guidance = '<div class="alert alert-info">' +
                        '<strong>Duplicate Scan Prevention</strong><br>' +
                        'Time in minutes that must pass before the same contractor can scan again.<br>' +
                        'Decimals are supported (e.g., 0.5 = 30 seconds).<br>' +
                        'Current: 2 minutes. Range: 0.1-60 minutes.' +
                        '</div>';
                    break;
                case 'HealthDeclarationWindowMinutes':
                    guidance = '<div class="alert alert-info">' +
                        '<strong>Health Declaration Window</strong><br>' +
                        'Time in minutes that contractors can change their health status after scanning.<br>' +
                        'Also controls how long the health declaration form remains visible.<br>' +
                        'Decimals are supported (e.g., 0.5 = 30 seconds).<br>' +
                        'Current: 2 minutes. Range: 0.1-60 minutes.' +
                        '</div>';
                    break;
                case 'AdminADGroup':
                    guidance = '<div class="alert alert-info">' +
                        '<strong>Admin Active Directory Group</strong><br>' +
                        'AD group name that grants admin access to this system.<br>' +
                        'Only users in this group can access the admin interface.' +
                        '</div>';
                    break;
                default:
                    guidance = '<div class="alert alert-secondary">Update the configuration value as needed.</div>';
            }

            $('#config-description').parent().find('.guidance').remove();
            $('#config-description').parent().append(guidance);
        }
    },

    // TimeLogs.cshtml - Time logs management
    timeLogs: {
        timeLogTable: null,

        init: function() {
            const self = this;

            // Initialize DataTable
            self.timeLogTable = $('#timelogs-table').DataTable({
                ajax: {
                    url: '/Admin/GetAllTimeLogs',
                    dataSrc: function(data) {
                        if (data.success && data.data) {
                            $('#no-data-message').hide();
                            return data.data;
                        }
                        $('#no-data-message').show();
                        return [];
                    }
                },
                columns: [
                    { data: 'attendance_id' },
                    { data: 'employee_id' },
                    {
                        data: 'time_in',
                        render: function(data) {
                            if (!data) return '-';
                            return new Date(data).toLocaleString();
                        }
                    },
                    {
                        data: 'time_out',
                        render: function(data) {
                            if (!data) return '<span class="badge bg-success">Active</span>';
                            return new Date(data).toLocaleString();
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
                            return new Date(data).toLocaleString();
                        }
                    },
                    {
                        data: null,
                        render: function(data) {
                            return `
                                <button class="btn btn-sm btn-edit" data-attendance-id="${data.attendance_id}">Edit</button>
                                <button class="btn btn-sm btn-delete" data-attendance-id="${data.attendance_id}">Delete</button>
                            `;
                        }
                    }
                ],
                order: [[2, 'desc']], // Sort by Time In descending
                pageLength: 25
            });

            // Apply filters button
            $('#apply-filters').on('click', function() {
                self.applyFilters();
            });

            // Clear filters button
            $('#clear-filters').on('click', function() {
                self.clearFilters();
            });

            // Edit timelog button
            $(document).on('click', '.btn-edit', function() {
                const attendanceId = $(this).data('attendance-id');

                $.ajax({
                    url: '/Admin/GetTimeLogById',
                    method: 'GET',
                    data: { attendance_id: attendanceId },
                    success: function(response) {
                        if (response.success && response.data) {
                            const timelog = response.data;

                            $('#edit_attendance_id').val(timelog.attendance_id);
                            $('#edit_employee_id').val(timelog.employee_id);
                            $('#edit_time_in').val(self.formatDateTimeForInput(timelog.time_in));
                            $('#edit_time_out').val(timelog.time_out ? self.formatDateTimeForInput(timelog.time_out) : '');
                            $('#edit_health_status').val(timelog.health_status);

                            $('#timelog-modal').modal('show');
                        } else {
                            AdminPage.common.showError('Failed to load timelog data');
                        }
                    },
                    error: function() {
                        AdminPage.common.showError('Failed to load timelog');
                    }
                });
            });

            // Delete timelog button
            $(document).on('click', '.btn-delete', function() {
                const attendanceId = $(this).data('attendance-id');

                AdminPage.common.showConfirmation(
                    `This will permanently delete attendance record ${attendanceId}. This action cannot be undone.`,
                    function() {
                        $.ajax({
                            url: '/Admin/DeleteTimeLog',
                            method: 'POST',
                            data: { attendance_id: attendanceId },
                            success: function(response) {
                                if (response.success) {
                                    AdminPage.common.showSuccess('Attendance record deleted successfully');
                                    self.timeLogTable.ajax.reload();
                                } else {
                                    AdminPage.common.showError(response.message);
                                }
                            },
                            error: function() {
                                AdminPage.common.showError('Failed to delete timelog');
                            }
                        });
                    }
                );
            });

            // Save timelog
            $('#save-timelog').on('click', function() {
                const timelog = {
                    attendance_id: parseInt($('#edit_attendance_id').val()),
                    employee_id: $('#edit_employee_id').val(),
                    time_in: $('#edit_time_in').val(),
                    time_out: $('#edit_time_out').val() || null,
                    health_status: $('#edit_health_status').val()
                };

                $.ajax({
                    url: '/Admin/EditTimeLog',
                    method: 'POST',
                    data: timelog,
                    success: function(response) {
                        if (response.success) {
                            $('#timelog-modal').modal('hide');
                            AdminPage.common.showSuccess('Attendance record updated successfully with audit trail');
                            self.timeLogTable.ajax.reload();
                        } else {
                            AdminPage.common.showError(response.message);
                        }
                    },
                    error: function() {
                        AdminPage.common.showError('Failed to update timelog');
                    }
                });
            });
        },

        applyFilters: function() {
            const employeeId = $('#filter-employee-id').val();
            const fromDate = $('#filter-from-date').val();
            const toDate = $('#filter-to-date').val();
            const healthStatus = $('#filter-health-status').val();

            // Reload DataTable with filters
            this.timeLogTable.ajax.url(this.buildFilterUrl(employeeId, fromDate, toDate, healthStatus)).load();
        },

        clearFilters: function() {
            $('#filter-employee-id').val('');
            $('#filter-from-date').val('');
            $('#filter-to-date').val('');
            $('#filter-health-status').val('');

            // Reload DataTable without filters
            this.timeLogTable.ajax.url('/Admin/GetAllTimeLogs').load();
        },

        buildFilterUrl: function(employeeId, fromDate, toDate, healthStatus) {
            let url = '/Admin/GetAllTimeLogs?';
            const params = [];

            if (employeeId) params.push('employee_id=' + encodeURIComponent(employeeId));
            if (fromDate) params.push('from_date=' + encodeURIComponent(fromDate));
            if (toDate) params.push('to_date=' + encodeURIComponent(toDate));
            if (healthStatus) params.push('health_status=' + encodeURIComponent(healthStatus));

            return url + params.join('&');
        },

        formatDateTimeForInput: function(dateString) {
            if (!dateString) return '';
            const date = new Date(dateString);
            // Format: YYYY-MM-DDTHH:mm for datetime-local input
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            const hours = String(date.getHours()).padStart(2, '0');
            const minutes = String(date.getMinutes()).padStart(2, '0');
            return `${year}-${month}-${day}T${hours}:${minutes}`;
        }
    },

    // Index.cshtml - Admin dashboard
    index: {
        init: function() {
            // Dashboard initialization if needed
            // Load statistics, charts, etc.
            console.log('Admin dashboard initialized');
        }
    }
};

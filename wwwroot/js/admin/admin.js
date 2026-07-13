// AdminPage - Organized JavaScript for Admin Controller views
// Structure: projects, contractors, systemConfig, timeLogs, index

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
        },

        formatDateForInput: function(dateString) {
            if (!dateString) return '';
            return dateString.split('T')[0];
        }
    },

    // Projects.cshtml - Projects CRUD
    projects: {
        projectTable: null,

        init: function() {
            const self = this;

            // Load providers into datalist on page load
            self.loadProvidersForDatalist();

            // Auto-fill provider_code when provider is selected from datalist
            $('#provider_name_dropdown').on('input', function() {
                const selectedValue = $(this).val();
                const $datalist = $('#provider-list');
                const matchingOption = $datalist.find('option[value="' + selectedValue + '"]');

                if (matchingOption.length > 0) {
                    // Existing provider found - auto-fill provider_code and make it readonly
                    const providerCode = matchingOption.data('provider-code');
                    $('#provider_code').val(providerCode).prop('readonly', true);
                } else {
                    // No matching provider - clear code and make field writable
                    $('#provider_code').val('').prop('readonly', false);
                }
            });

            // Initialize DataTable
            self.projectTable = $('#projects-table').DataTable({
                ajax: {
                    url: '/Admin/GetAllProjects',
                    dataSrc: function(data) {
                        return data.success ? data.data : [];
                    }
                },
                columns: [
                    {
                        data: null,
                        render: function(data) {
                            return `${data.project_name} (${data.project_code})`;
                        }
                    },
                    {
                        data: null,
                        render: function(data) {
                            return `${data.provider_name} (${data.provider_code})`;
                        }
                    },
                    {
                        data: null,
                        render: function(data) {
                            if (!data.contract_startdate || !data.contract_enddate) return '-';
                            const startDate = new Date(data.contract_startdate).toLocaleDateString();
                            const endDate = new Date(data.contract_enddate).toLocaleDateString();
                            return `${startDate} ~ ${endDate}`;
                        }
                    },
                    {
                        data: 'contractor_count',
                        render: function(data, type, row) {
                            const count = data || 0;
                            return `<a href="#" class="contractor-count text-center" data-project-code="${row.project_code}" style="cursor: pointer; text-decoration: underline; font-weight: bold;">${count}</a>`;
                        }
                    },
                    {
                        data: 'contract_enddate',
                        render: function(data) {
                            if (!data) return '-';
                            return self.calculateDueDate(data);
                        }
                    },
                    {
                        data: 'active',
                        render: function(data) {
                            if (data === 1) {
                                return '<span class="badge bg-success">Active</span>';
                            } else {
                                return '<span class="badge bg-secondary">Inactive</span>';
                            }
                        }
                    },
                    {
                        data: null,
                        render: function(data) {
                            return `
                                <button class="btn btn-sm btn-warning btn-edit" data-project-code="${data.project_code}">
                                    <i class="fa-regular fa-pen-to-square"></i>
                                </button>
                            `;
                        }
                    }
                ],
                pageLength: 25
            });


            // Export to Excel button
            $('#export-projects').on('click', function() {
                self.exportToExcel();
            });

            // Show export button when data exists
            self.projectTable.on('draw', function() {
                const hasData = self.projectTable.data().length > 0;
                $('#export-projects').toggle(hasData);
            });

            // Add project button
            $('#add-project').on('click', function() {
                $('#project-form')[0].reset();
                $('#project_code').val('');
                $('#project_code_field').hide();
                $('#project_code').prop('readonly', true);

                // Re-enable provider input for add mode
                $('#provider_name_dropdown').prop('disabled', false).prop('readonly', false);

                // Reset provider fields
                $('#provider_name_dropdown').val('');
                $('#provider_code').val('').prop('readonly', false); // Enable for add mode

                // Reload providers into datalist
                self.loadProvidersForDatalist();

                $('#project_name').val('');
                $('#provider_pic').val('');
                $('#provider_pic_number').val('');
                $('#contract_startdate').val('');
                $('#contract_enddate').val('');
                $('#project_active').prop('checked', true);
                $('#project_active_label').text('Active');
                $('#project-modal .modal-title').text('Add New Project');

                // Use Bootstrap 5 native API
                var modal = new bootstrap.Modal(document.getElementById('project-modal'));
                modal.show();
            });

            // Edit project button
            $(document).on('click', '.btn-edit', function() {
                const projectCode = $(this).data('project-code');
                const project = self.projectTable.row($(this).closest('tr')).data();

                $('#project_code').val(project.project_code);
                $('#project_code_field').show();
                $('#project_code').prop('readonly', true);

                // Set provider input and auto-fill code
                if (project.provider_code && project.provider_name) {
                    $('#provider_name_dropdown').val(project.provider_name);
                    $('#provider_code').val(project.provider_code);
                    $('#provider_code').prop('readonly', true); // Always readonly in edit mode
                }

                // Make provider input read-only in edit mode
                $('#provider_name_dropdown').prop('readonly', true);

                // Editable project fields
                $('#project_name').val(project.project_name);
                $('#provider_pic').val(project.provider_pic);
                $('#provider_pic_number').val(project.provider_pic_number);
                $('#contract_startdate').val(AdminPage.common.formatDateForInput(project.contract_startdate));
                $('#contract_enddate').val(AdminPage.common.formatDateForInput(project.contract_enddate));

                // Populate active toggle
                $('#project_active').prop('checked', project.active === 1);
                $('#project_active_label').text(project.active === 1 ? 'Active' : 'Inactive');

                $('#project-modal .modal-title').text('Edit Project');

                // Use Bootstrap 5 native API
                var modal = new bootstrap.Modal(document.getElementById('project-modal'));
                modal.show();
            });

            // Contractor count click handler
            $(document).on('click', '.contractor-count', function(e) {
                e.preventDefault();
                const projectCode = $(this).data('project-code');
                self.loadContractors(projectCode);
            });

            // Handle active toggle state change
            $('#project_active').on('change', function() {
                const isActive = $(this).is(':checked');
                $('#project_active_label').text(isActive ? 'Active' : 'Inactive');
            });

            // Save project
            $('#save-project').on('click', function() {
                // Client-side validation
                const projectName = $('#project_name').val().trim();
                if (!projectName) {
                    AdminPage.common.showError('Project Name is required');
                    return;
                }

                const providerCode = $('#provider_code').val().trim();
                const providerName = $('#provider_name_dropdown').val().trim();

                if (!providerName) {
                    AdminPage.common.showError('Provider Name is required');
                    return;
                }

                if (!providerCode) {
                    AdminPage.common.showError('Provider Code is required. Enter existing provider code or create a new one.');
                    return;
                }

                // Validate contract dates
                const startDate = $('#contract_startdate').val();
                const endDate = $('#contract_enddate').val();

                if (startDate && endDate) {
                    const start = new Date(startDate);
                    const end = new Date(endDate);

                    if (start > end) {
                        AdminPage.common.showError('Contract Start Date cannot be later than Contract End Date');
                        return;
                    }
                }

                const project = {
                    project_name: projectName,
                    provider_code: providerCode,
                    provider_name: providerName,
                    provider_pic: $('#provider_pic').val(),
                    provider_pic_number: $('#provider_pic_number').val(),
                    contract_startdate: $('#contract_startdate').val(),
                    contract_enddate: $('#contract_enddate').val(),
                    active: $('#project_active').is(':checked') ? 1 : 0
                };

                const isNewProject = !$('#project_code').val();
                const url = isNewProject ? '/Admin/CreateProject' : '/Admin/UpdateProject';

                // For existing projects, include project_code for updates
                if (!isNewProject) {
                    project.project_code = $('#project_code').val();
                }

                $.ajax({
                    url: url,
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(project),
                    success: function(response) {
                        if (response.success) {
                            // Use Bootstrap 5 native API
                            var modal = bootstrap.Modal.getInstance(document.getElementById('project-modal'));
                            if (modal) {
                                modal.hide();
                            }
                            AdminPage.common.showSuccess(response.message);
                            self.projectTable.ajax.reload();
                        } else {
                            AdminPage.common.showError(response.message);
                        }
                    },
                    error: function(xhr, status, error) {
                        AdminPage.common.showError('Failed to save project. Please try again.');
                        console.error('Save project error:', { xhr, status, error });
                    }
                });
            });

            // Initialize sidebar
            if (AdminPage.sidebar) {
                AdminPage.sidebar.init();
            }
        },

        loadProvidersForDatalist: function() {
            $.ajax({
                url: '/Admin/GetAllProviders',
                method: 'GET',
                success: function(response) {
                    if (response.success && response.data) {
                        const $datalist = $('#provider-list');
                        $datalist.empty();

                        response.data.forEach(provider => {
                            $datalist.append('<option value="' + provider.provider_name + '" data-provider-code="' + provider.provider_code + '">');
                        });
                    }
                },
                error: function() {
                    $('#provider-list').html('<option value="">Failed to load providers</option>');
                }
            });
        },

        calculateDueDate: function(contractEndDateString) {
            if (!contractEndDateString) return '-';

            const contractEndDate = new Date(contractEndDateString);
            const today = new Date();
            today.setHours(0, 0, 0, 0);

            const diffTime = contractEndDate - today;
            const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

            if (diffDays < 0) {
                return '<span style="color: red;">Expired</span>';
            } else if (diffDays <= 30) {
                return `<span style="color: orange;">${diffDays} days</span>`;
            } else {
                return `<span style="color: green;">${diffDays} days</span>`;
            }
        },

        loadContractors: function(projectCode) {
            const self = this;

            $.ajax({
                url: '/Admin/GetProjectContractors',
                method: 'GET',
                data: { project_code: projectCode },
                success: function(response) {
                    if (response.success && response.data) {
                        self.populateContractorsTable(response.data);

                        // Use Bootstrap 5 native API
                        var modal = new bootstrap.Modal(document.getElementById('contractors-modal'));
                        modal.show();
                    } else {
                        AdminPage.common.showError('Failed to load contractors.');
                    }
                },
                error: function(xhr, status, error) {
                    AdminPage.common.showError('Failed to load contractors. Please try again.');
                    console.error('Load contractors error:', { xhr, status, error });
                }
            });
        },

        populateContractorsTable: function(contractors) {
            const table = $('#contractors-table').DataTable({
                data: contractors,
                destroy: true,
                columns: [
                    { data: 'employee_id' },
                    { data: 'name' },
                    { data: 'position' },
                    { data: 'area_of_destination' }
                ],
                pageLength: 10
            });
        },

        exportToExcel: function() {
            // Get current filtered data from DataTable (respecting search and filters)
            const tableData = this.projectTable.rows({ search: 'applied' }).data().toArray();

            if (tableData.length === 0) {
                AdminPage.common.showWarning('No data available to export', 'No Data');
                return;
            }

            // Transform data for export with friendly column names
            const exportData = tableData.map(row => ({
                'Provider Code': row.provider_code,
                'Provider Name': row.provider_name,
                'Provider PIC': row.provider_pic || '',
                'PIC Contact Number': row.provider_pic_number || '',
                'Project Code': row.project_code,
                'Project Name': row.project_name,
                'Contract Start Date': row.contract_startdate ? AdminPage.common.formatDateTime(row.contract_startdate) : '',
                'Contract End Date': row.contract_enddate ? AdminPage.common.formatDateTime(row.contract_enddate) : '',
                'Active Contractors': row.contractor_count || 0,
                'Status': row.active === 1 ? 'Active' : 'Inactive'
            }));

            // Create Excel file using SheetJS
            const ws = XLSX.utils.json_to_sheet(exportData);
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, 'Projects');

            // Generate filename with timestamp
            const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, '-').replace('T', '_');
            const filename = `ContractorProjects_${timestamp}.xlsx`;

            // Download file
            XLSX.writeFile(wb, filename);

            // Show success message
            AdminPage.common.showSuccess(`Exported ${tableData.length} projects to Excel`, 'Export Successful');
        }
    },

    // Contractors.cshtml - Contractors CRUD
    contractors: {
        contractorTable: null,

        init: function() {
            const self = this;

            // Initialize Select2 for project dropdown
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

            // Load projects for dropdown
            self.loadProjects();

            // Add contractor button
            $('#add-contractor').on('click', function() {
                $('#contractor-form')[0].reset();
                $('#employee_id').val('');
                $('#project_code').val(null).trigger('change');
                $('#contractor-modal .modal-title').text('Add New Contractor');

                // Use Bootstrap 5 native API
                var modal = new bootstrap.Modal(document.getElementById('contractor-modal'));
                modal.show();
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
                $('#project_code').val(contractor.project_code).trigger('change');
                $('#position').val(contractor.position);

                $('#contractor-modal .modal-title').text('Edit Contractor');

                // Use Bootstrap 5 native API
                var modal = new bootstrap.Modal(document.getElementById('contractor-modal'));
                modal.show();
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
                            contentType: 'application/json',
                            data: JSON.stringify(contractor),
                            success: function(response) {
                                if (response.success) {
                                    AdminPage.common.showSuccess('Contractor set to inactive');
                                    self.contractorTable.ajax.reload();
                                } else {
                                    AdminPage.common.showError(response.message);
                                }
                            },
                            error: function(xhr, status, error) {
                                AdminPage.common.showError('Failed to set contractor inactive. Please try again.');
                                console.error('Set contractor inactive error:', { xhr, status, error });
                            }
                        });
                    }
                );
            });

            // Save contractor
            $('#save-contractor').on('click', function() {
                // Client-side validation
                const name = $('#name').val().trim();
                if (!name) {
                    AdminPage.common.showError('Name is required');
                    return;
                }

                const contractor = {
                    employee_id: $('#employee_id').val(),
                    name: name,
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
                            // Use Bootstrap 5 native API
                            var modal = bootstrap.Modal.getInstance(document.getElementById('contractor-modal'));
                            if (modal) {
                                modal.hide();
                            }
                            AdminPage.common.showSuccess(response.message);
                            self.contractorTable.ajax.reload();
                        } else {
                            AdminPage.common.showError(response.message);
                        }
                    },
                    error: function(xhr, status, error) {
                        AdminPage.common.showError('Failed to save contractor. Please try again.');
                        console.error('Save contractor error:', { xhr, status, error });
                    }
                });
            });

            // Initialize sidebar
            if (AdminPage.sidebar) {
                AdminPage.sidebar.init();
            }
        },

        loadProjects: function(providerCode = null) {
            $('#project_code').empty().append('<option value="">Select Project</option>');

            $.ajax({
                url: '/Admin/GetAllProjects',
                method: 'GET',
                success: function(response) {
                    if (response.success && response.data) {
                        response.data.forEach(project => {
                            // Show all projects (no provider filtering)
                            $('#project_code').append('<option value="' + project.project_code + '">' +
                                project.project_name + ' (' + project.project_code + ')</option>');
                        });
                    }
                }
            });
        },

        formatDateForInput: function(dateString) {
            if (!dateString) return '';
            return dateString.split('T')[0];
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

                // Use Bootstrap 5 native API
                var modal = new bootstrap.Modal(document.getElementById('config-modal'));
                modal.show();
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
                    contentType: 'application/json',
                    data: JSON.stringify(config),
                    success: function(response) {
                        if (response.success) {
                            // Use Bootstrap 5 native API
                            var modal = bootstrap.Modal.getInstance(document.getElementById('config-modal'));
                            if (modal) {
                                modal.hide();
                            }
                            AdminPage.common.showSuccess('Configuration updated successfully');
                            self.configTable.ajax.reload();
                        } else {
                            AdminPage.common.showError(response.message);
                        }
                    },
                    error: function(xhr, status, error) {
                        AdminPage.common.showError('Failed to update configuration. Please try again.');
                        console.error('Update config error:', { xhr, status, error });
                    }
                });
            });

            // Initialize sidebar
            if (AdminPage.sidebar) {
                AdminPage.sidebar.init();
            }
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

                            // Use Bootstrap 5 native API
                            var modal = new bootstrap.Modal(document.getElementById('timelog-modal'));
                            modal.show();
                        } else {
                            AdminPage.common.showError('Failed to load timelog data');
                        }
                    },
                    error: function(xhr, status, error) {
                        AdminPage.common.showError('Failed to load timelog');
                        console.error('Load timelog error:', { xhr, status, error });
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
                            contentType: 'application/json',
                            data: JSON.stringify({ attendance_id: attendanceId }),
                            success: function(response) {
                                if (response.success) {
                                    AdminPage.common.showSuccess('Attendance record deleted successfully');
                                    self.timeLogTable.ajax.reload();
                                } else {
                                    AdminPage.common.showError(response.message);
                                }
                            },
                            error: function(xhr, status, error) {
                                AdminPage.common.showError('Failed to delete timelog. Please try again.');
                                console.error('Delete timelog error:', { xhr, status, error });
                            }
                        });
                    }
                );
            });

            // Save timelog
            $('#save-timelog').on('click', function() {
                // Client-side validation
                const employeeId = $('#edit_employee_id').val().trim();
                if (!employeeId) {
                    AdminPage.common.showError('Employee ID is required');
                    return;
                }

                const timelog = {
                    attendance_id: parseInt($('#edit_attendance_id').val()),
                    employee_id: employeeId,
                    time_in: $('#edit_time_in').val(),
                    time_out: $('#edit_time_out').val() || null,
                    health_status: $('#edit_health_status').val()
                };

                $.ajax({
                    url: '/Admin/EditTimeLog',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(timelog),
                    success: function(response) {
                        if (response.success) {
                            // Use Bootstrap 5 native API
                            var modal = bootstrap.Modal.getInstance(document.getElementById('timelog-modal'));
                            if (modal) {
                                modal.hide();
                            }
                            AdminPage.common.showSuccess('Attendance record updated successfully with audit trail');
                            self.timeLogTable.ajax.reload();
                        } else {
                            AdminPage.common.showError(response.message);
                        }
                    },
                    error: function(xhr, status, error) {
                        AdminPage.common.showError('Failed to update timelog. Please try again.');
                        console.error('Update timelog error:', { xhr, status, error });
                    }
                });
            });

            // Initialize sidebar
            if (AdminPage.sidebar) {
                AdminPage.sidebar.init();
            }
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

    // Sidebar component - Admin sidebar navigation
    sidebar: {
        init: function() {
            const self = this;

            // Set active state based on current URL
            self.setActiveState();

            // Setup mobile toggle functionality
            self.setupMobileToggle();

            // Handle responsive behavior
            self.setupResponsiveBehavior();
        },

        setActiveState: function() {
            // Get current controller and action from URL
            const path = window.location.pathname;
            const pathParts = path.split('/').filter(part => part.length > 0);

            // Determine current page
            let currentPage = 'dashboard'; // default
            if (pathParts.length >= 2) {
                const controller = pathParts[pathParts.length - 2];
                const action = pathParts[pathParts.length - 1];

                if (controller === 'Admin') {
                    switch(action) {
                        case 'Index':
                            currentPage = 'dashboard';
                            break;
                        case 'Projects':
                            currentPage = 'projects';
                            break;
                        case 'Contractors':
                            currentPage = 'contractors';
                            break;
                        case 'SystemConfig':
                            currentPage = 'systemconfig';
                            break;
                        case 'TimeLogs':
                            currentPage = 'timelogs';
                            break;
                    }
                }
            }

            // Remove active class from all sidebar links
            $('.sidebar-link').removeClass('active');

            // Add active class to current page link
            const activeLinkMap = {
                'dashboard': 'a[href*="/Admin/Index"]',
                'projects': 'a[href*="/Admin/Projects"]',
                'contractors': 'a[href*="/Admin/Contractors"]',
                'systemconfig': 'a[href*="/Admin/SystemConfig"]',
                'timelogs': 'a[href*="/Admin/TimeLogs"]'
            };

            const activeSelector = activeLinkMap[currentPage];
            if (activeSelector) {
                $(activeSelector).addClass('active');
            }
        },

        setupMobileToggle: function() {
            // Add mobile toggle button if it doesn't exist
            if ($('.mobile-sidebar-toggle').length === 0) {
                const toggleButton = $('<button class="mobile-sidebar-toggle">' +
                    '<i class="fas fa-bars"></i>' +
                    '</button>');

                // Insert toggle button after navbar brand
                $('.navbar-brand').after(toggleButton);
            }

            // Handle toggle button click
            $(document).on('click', '.mobile-sidebar-toggle', function() {
                $('.admin-sidebar').toggleClass('show');
                $(this).find('i').toggleClass('fa-bars fa-times');
            });

            // Close sidebar when clicking outside on mobile
            $(document).on('click', function(e) {
                // Exclude Select2 elements to prevent dropdown interference
                if ($(e.target).closest('.select2-container').length ||
                    $(e.target).closest('.select2-dropdown').length) {
                    return;
                }

                if ($(window).width() < 768) {
                    if (!$(e.target).closest('.admin-sidebar').length &&
                        !$(e.target).closest('.mobile-sidebar-toggle').length) {
                        $('.admin-sidebar').removeClass('show');
                        $('.mobile-sidebar-toggle').find('i')
                            .removeClass('fa-times').addClass('fa-bars');
                    }
                }
            });

            // Close sidebar when clicking a navigation link on mobile
            $('.sidebar-link').on('click', function() {
                if ($(window).width() < 768) {
                    $('.admin-sidebar').removeClass('show');
                    $('.mobile-sidebar-toggle').find('i')
                        .removeClass('fa-times').addClass('fa-bars');
                }
            });
        },

        setupResponsiveBehavior: function() {
            const self = this;

            // Handle window resize
            $(window).on('resize', function() {
                if ($(window).width() >= 768) {
                    // Remove mobile-specific classes on desktop
                    $('.admin-sidebar').removeClass('show');
                    $('.mobile-sidebar-toggle').find('i')
                        .removeClass('fa-times').addClass('fa-bars');
                }
            });
        }
    },

    // Index.cshtml - Admin dashboard
    index: {
        init: function() {
            // Dashboard initialization
            console.log('Admin dashboard initialized');

            // Initialize sidebar
            if (AdminPage.sidebar) {
                AdminPage.sidebar.init();
            }
        }
    }
};

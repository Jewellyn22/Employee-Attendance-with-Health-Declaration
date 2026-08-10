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

        showWarning: function(message, title = 'Warning') {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'warning',
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

            // Keyboard navigation state variables
            let focusedIndex = -1;
            let isDatalistVisible = false;

            // Don't auto-show on focus - let user initiate typing
            $('#provider_name_dropdown').on('focus', function() {
                focusedIndex = -1;
                // Only show if there's already content
                if ($(this).val().length > 0) {
                    $('#provider-list').show();
                    isDatalistVisible = true;
                }
            });

            // Hide datalist when clicking outside
            $(document).on('click', function(e) {
                if (!$(e.target).closest('#provider_name_dropdown').length &&
                    !$(e.target).closest('#provider-list').length) {
                    $('#provider-list').hide();
                    isDatalistVisible = false;
                    focusedIndex = -1;
                }
            });

            // Handle keyboard navigation
            $('#provider_name_dropdown').on('keydown', function(e) {
                const $datalist = $('#provider-list');
                const $items = $datalist.find('li').not('[style*="display: none"]').not('.no-results');

                // Show datalist on arrow keys if hidden
                if (!isDatalistVisible && (e.key === 'ArrowDown' || e.key === 'ArrowUp')) {
                    e.preventDefault();
                    $datalist.show();
                    isDatalistVisible = true;
                    return;
                }

                switch(e.key) {
                    case 'ArrowDown':
                        e.preventDefault();
                        focusedIndex = Math.min(focusedIndex + 1, $items.length - 1);
                        updateFocusHighlight($items);
                        break;

                    case 'ArrowUp':
                        e.preventDefault();
                        focusedIndex = Math.max(focusedIndex - 1, 0);
                        updateFocusHighlight($items);
                        break;

                    case 'Enter':
                        if (focusedIndex >= 0 && focusedIndex < $items.length) {
                            e.preventDefault();
                            const $focusedItem = $($items[focusedIndex]);
                            selectProvider($focusedItem);
                        }
                        break;

                    case 'Escape':
                        e.preventDefault();
                        $datalist.hide();
                        isDatalistVisible = false;
                        focusedIndex = -1;
                        break;
                }
            });

            // Update visual highlighting for keyboard navigation
            function updateFocusHighlight($items) {
                $items.removeClass('keyboard-focused');
                if (focusedIndex >= 0 && focusedIndex < $items.length) {
                    $($items[focusedIndex]).addClass('keyboard-focused');
                    // Scroll into view if needed
                    const $item = $($items[focusedIndex]);
                    if ($item.length > 0) {
                        $item[0].scrollIntoView({ block: 'nearest' });
                    }
                }
            }

            // Select provider and update fields
            function selectProvider($item) {
                const providerName = $item.attr('value');
                const providerCode = $item.data('provider-code');

                $('#provider_name_dropdown').val(providerName);
                $('#provider_code').val(providerCode).prop('readonly', true);
                $('#provider-list').hide();
                isDatalistVisible = false;
                focusedIndex = -1;
            }

            // Auto-derive a provider code from the provider name
            // (e.g. "Alpha Circuits Inc" -> "ACI").
            // Field stays read-only; on collision the backend rejects and the admin renames the provider.
            function generateProviderCode(name) {
                if (!name) return '';
                const words = name.trim().toUpperCase()
                    .replace(/[^A-Z0-9\s]/g, ' ')   // strip punctuation/symbols
                    .split(/\s+/)
                    .filter(Boolean);

                if (words.length === 0) {
                    // Fallback: nothing alphanumeric -> timestamp-based code
                    return 'PRV' + String(Date.now()).slice(-5);
                }

                // Primary: first letter of every word -> "Alpha Circuits Inc" = "ACI"
                let code = words.map(function(w) { return w[0]; }).join('');

                // Fallback: initials too short (single short word) -> consonants of the name
                if (code.length < 2) {
                    const consonants = words.join('').replace(/[AEIOU0-9]/g, '');
                    code = (consonants || words.join('')).substring(0, 4);
                }

                return code.substring(0, 10); // sane cap well under varchar(50)
            }

            // Handle input with filtering, auto-show, and auto-fill provider_code
            $('#provider_name_dropdown').on('input', function() {
                const selectedValue = $(this).val();
                const $datalist = $('#provider-list');
                const $items = $datalist.find('li').not('.no-results');

                // Show dropdown when user starts typing
                if (selectedValue.length > 0 && $datalist.is(':hidden')) {
                    $datalist.show();
                    isDatalistVisible = true;
                }

                // Hide dropdown if input is cleared
                if (selectedValue.length === 0) {
                    $datalist.hide();
                    isDatalistVisible = false;
                    focusedIndex = -1;
                    $('#provider_code').val('');   // clear any stale auto-generated code
                    return;
                }

                // Reset keyboard navigation when user types
                focusedIndex = -1;
                $items.removeClass('keyboard-focused');

                // Filter options as user types
                let hasVisibleResults = false;
                $items.each(function() {
                    const $item = $(this);
                    const text = $item.text().toLowerCase();
                    const filter = selectedValue.toLowerCase();

                    if (text.includes(filter)) {
                        $item.show();
                        hasVisibleResults = true;
                    } else {
                        $item.hide();
                    }
                });

                // Show/hide "No results found" message
                const $noResults = $datalist.find('.no-results');
                if (!hasVisibleResults) {
                    //if ($noResults.length === 0) {
                    //    $datalist.append('<li class="no-results">No providers found. Type to add new provider.</li>');
                    //} else {
                    //    $noResults.show();
                    //}
                    if ($noResults.length === 0) {
                        $noResults.show();
                    }
                } else {
                    $noResults.hide();
                }

                // Check for exact match to auto-fill provider_code
                const matchingItem = $items.filter(function() {
                    return $(this).attr('value') === selectedValue;
                });

                if (matchingItem.length > 0) {
                    const providerCode = matchingItem.data('provider-code');
                    $('#provider_code').val(providerCode).prop('readonly', true);
                } else {
                    // New provider: auto-generate a read-only code from the name
                    $('#provider_code').val(generateProviderCode(selectedValue)).prop('readonly', true);
                }
            });

            // Handle click on provider list items
            $(document).on('click', '#provider-list li', function() {
                if (!$(this).hasClass('no-results')) {
                    selectProvider($(this));
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


            // Client-side Active/Inactive filter (DataTables custom search).
            // Guarded to #projects-table so it never affects another table.
            $.fn.dataTable.ext.search.push(function(settings, searchData, index, rowData) {
                if (settings.nTable.id !== 'projects-table') return true;
                const filterVal = $('#filter-project-status').val();
                if (!filterVal) return true;                          // "All" -> show every row
                return String(rowData.active) === String(filterVal);  // "1"=Active, "0"=Inactive
            });

            // Re-run the filter whenever the dropdown changes
            $('#filter-project-status').on('change', function() {
                self.projectTable.draw();
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
                $('#provider_code').val('').prop('readonly', true); // Read-only in add mode (auto-filled from Provider Name)

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
                            $datalist.append('<li value="' + provider.provider_name + '" data-provider-code="' + provider.provider_code + '">' + provider.provider_name + '</li>');
                        });
                    }
                },
                error: function() {
                    $('#provider-list').html('<li value="">Failed to load providers</li>');
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
        isEditing: false,
        currentEditingProject: null,

        init: function() {
            const self = this;

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
                            return row.project_name || data;   // show Project Name; fall back to code
                        }
                    },
                    { data: 'position' },
                    { data: 'area_of_destination' },
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
                                <button class="btn btn-sm btn-warning btn-edit" data-employee-id="${data.employee_id}">
                                    <i class="fa-regular fa-pen-to-square"></i>
                                </button>
                            `;
                        }
                    }
                ],
                pageLength: 25
            });

            // Client-side Active/Inactive filter (DataTables custom search).
            // Guarded to #contractors-table so it never affects another table.
            $.fn.dataTable.ext.search.push(function(settings, searchData, index, rowData) {
                if (settings.nTable.id !== 'contractors-table') return true;
                const filterVal = $('#filter-active-status').val();
                if (!filterVal) return true;                          // "All" -> show every row
                return String(rowData.active) === String(filterVal);  // "1"=Active, "0"=Inactive
            });

            // Re-run the filter whenever the dropdown changes
            $('#filter-active-status').on('change', function() {
                self.contractorTable.draw();
            });

            // Initialize Select2 and setup modal event handler
            $('#contractor-modal').on('shown.bs.modal', function() {
                // Initialize Select2 with dropdownParent configuration for Bootstrap modal
                // Initialize Select2 with dropdownParent for Bootstrap modal
                if (!$('#project_code').data('select2')) {
                    $('#project_code').select2({
                        placeholder: 'Select Project',
                        allowClear: true,
                        width: '100%',
                        dropdownParent: $('#contractor-modal')
                    });
                }

                // Always load providers (for both add and edit modes)
                self.loadProviders(self.currentEditingProvider);

                if (self.isEditing) {
                    // Edit mode: load this provider's projects and pre-select the current project
                    self.loadProjectsByProvider(self.currentEditingProvider, self.currentEditingProject);
                    // Lock Provider and Project so the contractor can't be reassigned mid-contract
                    $('#provider_code').prop('disabled', true);
                    $('#project_code').prop('disabled', true).trigger('change'); // Select2 reflects disabled state
                }
                // Add mode: project dropdown stays disabled ("Select Provider first") until a provider is chosen

                // Trigger resize to ensure Select2 recalculates position
                $(window).trigger('resize');

                // Reset editing variables
                self.currentEditingProject = null;
                self.currentEditingProvider = null;
            });

            // Add contractor button
            $('#add-contractor').on('click', function() {
                self.isEditing = false;
                $('#contractor-form')[0].reset();

                // Employee ID is auto-generated on save - hide the field for new contractors
                $('#employee_id').val('');
                $('#employee_id_field').hide();

                // Reset provider; project depends on provider, so disable it until one is chosen
                $('#provider_code').val('');
                $('#provider_code').prop('disabled', false);
                $('#project_code').empty().append('<option value="">Select Provider first</option>')
                    .val(null).trigger('change.select2').prop('disabled', true);

                // Reset toggle to Active state for new contractors
                $('#contractor_active').prop('checked', true);
                $('#contractor_active_label').text('Active');

                $('#contractor-modal .modal-title').text('Add New Contractor');

                // Use Bootstrap 5 native API
                var modal = new bootstrap.Modal(document.getElementById('contractor-modal'));
                modal.show();
            });

            // Edit contractor button
            $(document).on('click', '.btn-edit', function() {
                const employeeId = $(this).data('employee-id');
                const contractor = self.contractorTable.row($(this).closest('tr')).data();

                self.isEditing = true;

                // Store current provider/project for the filtered cascade load
                self.currentEditingProject = contractor.project_code;
                self.currentEditingProvider = contractor.provider_code;

                // Employee ID is auto-generated and read-only
                $('#employee_id').val(contractor.employee_id);
                $('#employee_id').prop('readonly', true);
                $('#employee_id_field').show();
                $('#name').val(contractor.name);
                $('#gender').val(contractor.gender);
                $('#birthdate').val(self.formatDateForInput(contractor.birthdate));
                $('#contact_number').val(contractor.contact_number);
                $('#area_of_destination').val(contractor.area_of_destination);
                $('#provider_code').val(contractor.provider_code);
                $('#position').val(contractor.position);

                // Set toggle state based on contractor status
                $('#contractor_active').prop('checked', contractor.active === 1);
                $('#contractor_active_label').text(contractor.active === 1 ? 'Active' : 'Inactive');

                $('#contractor-modal .modal-title').text('Edit Contractor');

                // Use Bootstrap 5 native API
                var modal = new bootstrap.Modal(document.getElementById('contractor-modal'));
                modal.show();
            });

            // Toggle state change handler
            $('#contractor_active').on('change', function() {
                const isActive = $(this).is(':checked');
                $('#contractor_active_label').text(isActive ? 'Active' : 'Inactive');
            });

            // Provider change -> reload projects filtered by the selected provider (cascade)
            $('#provider_code').on('change', function() {
                const providerCode = $(this).val();
                if (providerCode) {
                    self.loadProjectsByProvider(providerCode, null);
                } else {
                    // No provider selected: clear and disable the project dropdown
                    $('#project_code').empty().append('<option value="">Select Provider first</option>')
                        .val(null).trigger('change.select2').prop('disabled', true);
                }
            });

            // Save contractor
            $('#save-contractor').on('click', function() {
                // Client-side validation
                const name = $('#name').val().trim();
                if (!name) {
                    AdminPage.common.showError('Name is required');
                    return;
                }

                const gender = $('#gender').val();
                if (!gender) {
                    AdminPage.common.showError('Gender is required');
                    return;
                }

                const providerCode = $('#provider_code').val();
                if (!providerCode) {
                    AdminPage.common.showError('Provider is required');
                    return;
                }

                const projectCode = $('#project_code').val();
                if (!projectCode) {
                    AdminPage.common.showError('Project is required');
                    return;
                }

                // Birthdate: required, not a future date, and at least 18 years old (DOLE)
                const birthdateStr = $('#birthdate').val();
                if (!birthdateStr) {
                    AdminPage.common.showError('Birthdate is required');
                    return;
                }
                const birthdate = new Date(birthdateStr);
                const today = new Date();
                today.setHours(0, 0, 0, 0);
                if (birthdate > today) {
                    AdminPage.common.showError('Birthdate cannot be a future date');
                    return;
                }
                let age = today.getFullYear() - birthdate.getFullYear();
                const monthDiff = today.getMonth() - birthdate.getMonth();
                if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthdate.getDate())) {
                    age--;
                }
                if (age < 18) {
                    AdminPage.common.showError('The employee is under 18 years old and is not eligible for employment under DOLE regulations.');
                    return;
                }

                const contractor = {
                    name: name,
                    gender: gender,
                    birthdate: birthdateStr,
                    contact_number: $('#contact_number').val(),
                    address: '',
                    area_of_destination: $('#area_of_destination').val(),
                    provider_code: providerCode,
                    project_code: projectCode,
                    position: $('#position').val(),
                    active: $('#contractor_active').is(':checked') ? 1 : 0
                };

                // employee_id is auto-generated on create; include it only when editing
                if (self.isEditing) {
                    contractor.employee_id = $('#employee_id').val();
                }

                const url = self.isEditing ? '/Admin/UpdateContractor' : '/Admin/CreateContractor';

                $.ajax({
                    url: url,
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(contractor),
                    success: function(response) {
                        if (response.success) {
                            // Use Bootstrap 5 native API
                            var modal = bootstrap.Modal.getInstance(document.getElementById('contractor-modal'));
                            if (modal) {
                                modal.hide();
                            }
                            // Surface the auto-generated Employee ID after create
                            const empIdSuffix = (!self.isEditing && response.data && response.data.employee_id)
                                ? ' (Employee ID: ' + response.data.employee_id + ')'
                                : '';
                            AdminPage.common.showSuccess(response.message + empIdSuffix);
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

        loadProjects: function(selectedProjectCode = null) {
            const $dropdown = $('#project_code');

            // Clear existing options
            $dropdown.empty().append('<option value="">Select Project</option>');

            $.ajax({
                url: '/Admin/GetAllProjects',
                method: 'GET',
                success: function(response) {
                    if (response.success && response.data) {
                        console.log('Loading projects:', response.data.length, 'projects found');

                        response.data.forEach(project => {
                            $dropdown.append('<option value="' + project.project_code + '">' +
                                project.project_name + ' (' + project.project_code + ')</option>');
                        });

                        // Set selected project if provided (for edit mode)
                        if (selectedProjectCode) {
                            $dropdown.val(selectedProjectCode).trigger('change.select2');
                            console.log('Project selected:', selectedProjectCode);
                        }

                        // Notify Select2 that options have changed
                        $dropdown.trigger('change.select2');

                        console.log('Projects loaded successfully');
                    } else {
                        console.error('Invalid response format:', response);
                    }
                },
                error: function(xhr, status, error) {
                    console.error('Failed to load projects:', { xhr, status, error });
                    AdminPage.common.showError('Failed to load projects. Please try again.');
                }
            });
        },

        loadProjectsByProvider: function(providerCode, selectedProjectCode) {
            const $dropdown = $('#project_code');
            $dropdown.prop('disabled', false);
            $dropdown.empty().append('<option value="">Select Project</option>');

            if (!providerCode) {
                $dropdown.val(null).trigger('change.select2');
                return;
            }

            $.ajax({
                url: '/Admin/GetProjectsByProvider',
                method: 'GET',
                data: { provider_code: providerCode },
                success: function(response) {
                    if (response.success && response.data) {
                        const today = new Date();
                        today.setHours(0, 0, 0, 0);

                        response.data.forEach(function(project) {
                            // Skip expired projects (contract_enddate in the past).
                            // Null enddate = open-ended contract (not expired).
                            const isExpired = project.contract_enddate && new Date(project.contract_enddate) < today;
                            // In edit mode, always keep the contractor's currently-assigned project visible.
                            const isCurrentAssignment = selectedProjectCode && project.project_code === selectedProjectCode;
                            if (isExpired && !isCurrentAssignment) {
                                return;
                            }
                            $dropdown.append('<option value="' + project.project_code + '">' +
                                project.project_name + ' (' + project.project_code + ')</option>');
                        });

                        if (selectedProjectCode) {
                            $dropdown.val(selectedProjectCode).trigger('change.select2');
                        } else {
                            $dropdown.val(null).trigger('change.select2');
                        }
                    } else {
                        $dropdown.val(null).trigger('change.select2');
                    }
                },
                error: function(xhr, status, error) {
                    console.error('Failed to load projects for provider:', { xhr, status, error });
                    AdminPage.common.showError('Failed to load projects. Please try again.');
                }
            });
        },

        loadProviders: function(selectedProviderCode = null) {
            const $dropdown = $('#provider_code');

            // Clear existing options
            $dropdown.empty().append('<option value="">Select Provider</option>');

            $.ajax({
                url: '/Admin/GetAllProviders',
                method: 'GET',
                success: function(response) {
                    if (response.success && response.data) {
                        console.log('Loading providers:', response.data.length, 'providers found');

                        response.data.forEach(provider => {
                            $dropdown.append('<option value="' + provider.provider_code + '">' +
                                provider.provider_name + ' (' + provider.provider_code + ')</option>');
                        });

                        // Set selected provider if provided (for edit mode)
                        if (selectedProviderCode) {
                            $dropdown.val(selectedProviderCode).trigger('change.select2');
                            console.log('Provider selected:', selectedProviderCode);
                        }

                        // Notify Select2 that options have changed
                        $dropdown.trigger('change.select2');

                        console.log('Providers loaded successfully');
                    } else {
                        console.error('Invalid response format:', response);
                    }
                },
                error: function(xhr, status, error) {
                    console.error('Failed to load providers:', { xhr, status, error });
                    AdminPage.common.showError('Failed to load providers. Please try again.');
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
                        if (!data.success) return [];
                        // Filter out AdminADGroup - admins cannot edit LDAP security settings
                        return data.data.filter(config => config.key !== 'AdminADGroup');
                    }
                },
                columns: [
                    { data: 'key' },
                    { data: 'value' },
                    { data: 'description' },
                    {
                        data: null,
                        render: function(data) {
                            // Show toggle switch for ScanInputReadOnly boolean config
                            if (data.key === 'ScanInputReadOnly') {
                                const isChecked = data.value === 'true' || data.value === '1' || data.value === true;
                                return `
                                    <div class="form-check form-switch mb-0">
                                        <input type="checkbox"
                                               class="form-check-input config-toggle"
                                               data-config-key="${data.key}"
                                               ${isChecked ? 'checked' : ''}
                                               style="cursor: pointer;">
                                    </div>
                                `;
                            }

                            // Show edit button for other configs
                            return `
                                <button class="btn btn-sm btn-warning btn-edit" data-config-key="${data.key}">
                                    <i class="fa-regular fa-pen-to-square"></i>
                                </button>
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
                if (config.key === 'DebounceThresholdSeconds' || config.key === 'HealthDeclarationWindowSeconds') {
                    const numValue = parseFloat(config.value);
                    if (isNaN(numValue) || numValue < 5 || numValue > 300) {
                        AdminPage.common.showError('Value must be between 5 and 300 seconds', 'Validation Error');
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

            // Handle direct toggle change for boolean configs
            $(document).on('change', '.config-toggle', function() {
                const $toggle = $(this);
                const configKey = $toggle.data('config-key');
                const newValue = $toggle.is(':checked') ? 'true' : 'false';

                // Visual feedback - disable toggle during save
                $toggle.prop('disabled', true);

                $.ajax({
                    url: '/Admin/UpdateSystemConfig',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({
                        key: configKey,
                        value: newValue
                    }),
                    success: function(response) {
                        if (response.success) {
                            AdminPage.common.showSuccess('Configuration updated successfully');
                            // No need to reload - toggle already reflects new state
                        } else {
                            // Revert toggle on error
                            $toggle.prop('checked', !$toggle.is(':checked'));
                            AdminPage.common.showError(response.message);
                        }
                    },
                    error: function(xhr, status, error) {
                        // Revert toggle on error
                        $toggle.prop('checked', !$toggle.is(':checked'));
                        AdminPage.common.showError('Failed to update configuration. Please try again.');
                        console.error('Update config error:', { xhr, status, error });
                    },
                    complete: function() {
                        // Re-enable toggle
                        $toggle.prop('disabled', false);
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
                case 'DebounceThresholdSeconds':
                    guidance = '<div class="alert alert-info">' +
                        '<strong>Duplicate Scan Prevention</strong><br>' +
                        'Time in seconds that must pass before the same contractor can scan again.<br>' +
                        'Current: 30 seconds. Range: 5-300 seconds.' +
                        '</div>';
                    break;
                case 'HealthDeclarationWindowSeconds':
                    guidance = '<div class="alert alert-info">' +
                        '<strong>Health Declaration Window</strong><br>' +
                        'Time in seconds that contractors can change their health status after scanning.<br>' +
                        'Also controls how long the health declaration form remains visible.<br>' +
                        'Current: 30 seconds. Range: 5-300 seconds.' +
                        '</div>';
                    break;
                case 'AdminADGroup':
                    guidance = '<div class="alert alert-info">' +
                        '<strong>Admin Active Directory Group</strong><br>' +
                        'AD group name that grants admin access to this system.<br>' +
                        'Only users in this group can access the admin interface.' +
                        '</div>';
                    break;
                case 'ScanInputReadOnly':
                    guidance = '<div class="alert alert-info">' +
                        '<strong>Scan Input Read-Only Mode</strong><br>' +
                        'When enabled (toggle ON), employee ID field only accepts scanner input.<br>' +
                        'When disabled (toggle OFF), manual typing is allowed in addition to scanning.' +
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

            // Export to Excel button
            $('#export-timelogs').on('click', function() {
                self.exportToExcel();
            });

            // Show export button only when data exists
            self.timeLogTable.on('draw', function() {
                const hasData = self.timeLogTable.data().length > 0;
                $('#export-timelogs').toggle(hasData);
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
        },

        exportToExcel: function() {
            // Get current filtered data from DataTable (respecting search and filters)
            const tableData = this.timeLogTable.rows({ search: 'applied' }).data().toArray();

            if (tableData.length === 0) {
                AdminPage.common.showWarning('No data available to export', 'No Data');
                return;
            }

            // Transform data for export with friendly column names (matches visible table)
            const exportData = tableData.map(row => ({
                'Attendance ID': row.attendance_id,
                'Employee ID': row.employee_id,
                'Time In': row.time_in ? AdminPage.common.formatDateTime(row.time_in) : '-',
                'Time Out': row.time_out ? AdminPage.common.formatDateTime(row.time_out) : 'Active',
                'Health Status': row.health_status,
                'Updated By': row.updated_by || '-',
                'Updated At': row.updated_at ? AdminPage.common.formatDateTime(row.updated_at) : '-'
            }));

            // Create Excel file using SheetJS
            const ws = XLSX.utils.json_to_sheet(exportData);
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, 'TimeLogs');

            // Generate filename with timestamp
            const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, '-').replace('T', '_');
            const filename = `TimeLogs_${timestamp}.xlsx`;

            // Download file
            XLSX.writeFile(wb, filename);

            // Show success message
            AdminPage.common.showSuccess(`Exported ${tableData.length} records to Excel`, 'Export Successful');
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

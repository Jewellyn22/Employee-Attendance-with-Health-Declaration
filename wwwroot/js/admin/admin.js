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
                    { data: 'area_of_destination' },
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
                        className: 'text-center',
                        render: function(data) {
                            return `
                                <button class="btn btn-sm btn-warning btn-edit" data-project-code="${data.project_code}">
                                    <i class="fa-regular fa-pen-to-square"></i>
                                </button>
                                <button class="btn btn-sm btn-danger btn-delete-project" data-project-code="${data.project_code}" data-contractor-count="${data.contractor_count || 0}" title="Delete Project">
                                    <i class="fa-regular fa-trash-can"></i>
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

            // Export project contractors (modal) button
            $('#export-project-contractors').on('click', function() {
                self.exportContractorsToExcel();
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
                $('#project_area_of_destination').val('');
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
                $('#project_area_of_destination').val(project.area_of_destination || '');
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

            // Delete project button (soft delete: enrolled employees whose LAST active
            // project is this one are marked deleted first, then the project — data is
            // retained in the database; employees still on another active project survive)
            $(document).on('click', '.btn-delete-project', function() {
                const projectCode = $(this).data('project-code');
                const contractorCount = $(this).data('contractor-count') || 0;

                let message = `Are you sure you want to delete project ${projectCode}?`;
                if (contractorCount > 0) {
                    message = `Delete project ${projectCode}? Enrolled employee(s) without another active project (up to ${contractorCount}) will also be deleted and will no longer be able to scan at the kiosk. Employees still assigned to another active project are kept.`;
                }

                AdminPage.common.showConfirmation(message, function() {
                    $.ajax({
                        url: '/Admin/DeleteProject',
                        method: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify({ project_code: projectCode }),
                        success: function(response) {
                            if (response.success) {
                                AdminPage.common.showSuccess(response.message);
                                self.projectTable.ajax.reload();
                            } else {
                                AdminPage.common.showError(response.message);
                            }
                        },
                        error: function(xhr, status, error) {
                            AdminPage.common.showError('Failed to delete project. Please try again.');
                            console.error('Delete project error:', { xhr, status, error });
                        }
                    });
                });
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

                const projectArea = $('#project_area_of_destination').val().trim();
                if (!projectArea) {
                    AdminPage.common.showError('Area of Destination is required');
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

                const providerPic = $('#provider_pic').val().trim();
                if (!providerPic) {
                    AdminPage.common.showError('Provider Project PIC is required');
                    return;
                }

                const contactNo = $('#provider_pic_number').val().trim();
                if (!contactNo) {
                    AdminPage.common.showError('Contact No is required');
                    return;
                }

                // Validate contract dates
                const startDate = $('#contract_startdate').val();
                const endDate = $('#contract_enddate').val();

                if (!startDate) {
                    AdminPage.common.showError('Contract Start Date is required');
                    return;
                }

                if (!endDate) {
                    AdminPage.common.showError('Contract End Date is required');
                    return;
                }

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
                    provider_pic: providerPic,
                    provider_pic_number: contactNo,
                    area_of_destination: projectArea,
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
            this.currentProjectCode = projectCode;

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
            this.contractorsTable = $('#contractors-table').DataTable({
                data: contractors,
                destroy: true,
                columns: [
                    { data: 'employee_id' },
                    { data: 'name' },
                    { data: 'position' }
                ],
                pageLength: 10
            });
        },

        exportContractorsToExcel: function() {
            // Get current filtered data from modal DataTable (respecting search)
            const tableData = this.contractorsTable.rows({ search: 'applied' }).data().toArray();

            if (tableData.length === 0) {
                AdminPage.common.showWarning('No data available to export', 'No Data');
                return;
            }

            // Transform data for export with friendly column names
            const exportData = tableData.map(row => ({
                'Employee ID': row.employee_id,
                'Name': row.name,
                'Position': row.position
            }));

            // Create Excel file using SheetJS
            const ws = XLSX.utils.json_to_sheet(exportData);
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, 'Contractors');

            // Generate filename with timestamp
            const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, '-').replace('T', '_');
            const filename = `ProjectContractors_${this.currentProjectCode || 'Export'}_${timestamp}.xlsx`;

            // Download file
            XLSX.writeFile(wb, filename);

            // Show success message
            AdminPage.common.showSuccess(`Exported ${tableData.length} contractors to Excel`, 'Export Successful');
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
                'Area of Destination': row.area_of_destination || '',
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
        currentEditingAssignments: [],   // [{project_code, position}] of the contractor being edited (add-row picker pre-fill)
        availableProjects: [],   // provider's projects cached for the add-row picker (code, name, contract_enddate)
        skipProjectCascade: false,   // suppress cascade during programmatic provider set (edit init)
        selectedContractorIds: new Set(),   // checked employee_ids; survives pagination/filters, cleared after delete

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
                    {
                        data: null,
                        orderable: false,
                        searchable: false,
                        className: 'text-center',
                        render: function(data, type, row) {
                            // Re-emitted from the Set on every draw, so checked state survives
                            // pagination, sorting, filters, and ajax.reload().
                            const checked = self.selectedContractorIds.has(row.employee_id) ? ' checked' : '';
                            return '<input type="checkbox" class="contractor-select" data-employee-id="' + row.employee_id + '"' + checked + '>';
                        }
                    },
                    { data: 'employee_id' },
                    { data: 'name' },
                    {
                        // One line per assigned project: "Name (Position - Area)", LF-joined by
                        // the SP ('\n' = Excel Alt+Enter). Display swaps LF for <br> (raw, like
                        // the other unescaped renders); sort/search use the raw text. Fallback
                        // to the names/codes CSV for zero-assignment rows or pre-script data.
                        data: 'project_details',
                        render: function(data, type, row) {
                            const text = data || row.project_names || row.project_codes || '';
                            if (type === 'display') {
                                return String(text).split('\n').join('<br>');
                            }
                            return text;
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
                        className: 'text-center',
                        render: function(data) {
                            return `
                                <button class="btn btn-sm btn-warning btn-edit" data-employee-id="${data.employee_id}">
                                    <i class="fa-regular fa-pen-to-square"></i>
                                </button>
                                <button class="btn btn-sm btn-danger btn-delete-contractor" data-employee-id="${data.employee_id}" title="Delete">
                                    <i class="fa-regular fa-trash-can"></i>
                                </button>
                            `;
                        }
                    }
                ],
                pageLength: 25,
                initComplete: function() {
                    // Build the Project filter dropdown from the loaded data. Extracted
                    // into a reusable method so it can also run after ajax.reload().
                    self._populateProjectFilter();
                }
            });

            // Client-side Status + Project filters (DataTables custom search).
            // Guarded to #contractors-table so it never affects another table.
            $.fn.dataTable.ext.search.push(function(settings, searchData, index, rowData) {
                if (settings.nTable.id !== 'contractors-table') return true;

                const statusVal = $('#filter-active-status').val();
                if (statusVal && String(rowData.active) !== String(statusVal)) return false;

                const projectVal = $('#filter-project').val();
                // Multi-project: project_codes is a CSV — exact-match against the
                // split list so "ACI-26-001" doesn't match "ACI-26-0010".
                if (projectVal && !(rowData.project_codes || '').split(',').map(c => c.trim()).includes(projectVal)) return false;

                return true;
            });

            // Re-run the filters whenever either dropdown changes
            $('#filter-active-status').on('change', function() {
                self.contractorTable.draw();
            });
            $('#filter-project').on('change', function() {
                self.contractorTable.draw();
            });

            // Reset all filters (Project + Status dropdowns and global search) and redraw
            $('#reset-contractor-filters').on('click', function() {
                $('#filter-project').val('');
                $('#filter-active-status').val('');
                self.contractorTable.search('').draw();
            });

            // Export to Excel button
            $('#export-contractors').on('click', function() {
                self.exportToExcel();
            });

            // Bulk import modal + handlers
            self.initBulkImport();

            // Show export button only when data exists
            self.contractorTable.on('draw', function() {
                const hasData = self.contractorTable.data().length > 0;
                $('#export-contractors').toggle(hasData);
            });

            // Keep row checkboxes, header select-all, and the Delete Selected button in
            // sync after every draw (pagination, sort, filter, ajax.reload).
            self.contractorTable.on('draw', function() {
                self._syncContractorCheckboxState();
            });

            // Add-row picker: append/remove assignment rows (project select + position
            // input). Delegated so rows created later are covered automatically.
            $('#contractor-projects-container').on('click', '.cpa-remove', function() {
                self.removeProjectRow($(this).closest('.contractor-assignment-row'));
            });
            $('#contractor-projects-container').on('change', '.cpa-project', function() {
                // A row's project choice changes which projects the OTHER rows may offer
                self.rebuildRowOptions();
            });
            $('#add-project-row').on('click', function() {
                if ($('#contractor-projects-container .contractor-assignment-row').length >= 50) {
                    AdminPage.common.showError('A contractor can be assigned to at most 50 projects');
                    return;
                }
                self.addProjectRow(null, '');
            });

            // Tear the picker down when the modal closes so the next open starts clean
            // (Select2 instances destroyed; container emptied).
            $('#contractor-modal').on('hidden.bs.modal', function() {
                self.resetProjectRows();
            });

            // Setup modal event handler
            $('#contractor-modal').on('shown.bs.modal', function() {
                // Always load providers (for both add and edit modes).
                // In edit mode, suppress the provider->project cascade while we programmatically
                // set the provider value, so it doesn't fire a second picker rebuild
                // that would clobber the pre-filled rows.
                if (self.isEditing) {
                    self.skipProjectCascade = true;
                }
                // Capture before the async callback: the editing vars are reset at the end of
                // this handler, but loadProviders' callback runs only after its AJAX completes
                // — reading self.currentEditing* there would see the reset nulls/empties.
                const editProvider = self.currentEditingProvider;
                const editAssignments = self.currentEditingAssignments;
                self.loadProviders(editProvider, self.isEditing ? function () {
                    self.skipProjectCascade = false;
                    // Projects stay editable in edit mode (multi-project assignments):
                    // cache the provider's project list, then pre-fill one picker row per
                    // current {project, position} assignment.
                    self.fetchProjectsForProvider(editProvider, function () {
                        self.resetProjectRows();
                        editAssignments.forEach(function(a) {
                            self.addProjectRow(a.project_code, a.position);
                        });
                        if (!editAssignments.length) {
                            self.addProjectRow(null, '');
                        }
                    });
                } : null);

                if (self.isEditing) {
                    // Provider is locked once a contractor is registered (employee_id is
                    // provider-scoped); assignments remain editable via the picker rows.
                    $('#provider_code').prop('disabled', true);
                }
                // Add mode: picker stays empty until a provider is chosen (cascade adds rows)

                // Trigger resize to ensure Select2 recalculates position
                $(window).trigger('resize');

                // Reset editing variables
                self.currentEditingAssignments = [];
                self.currentEditingProvider = null;
            });

            // Add contractor button
            $('#add-contractor').on('click', function() {
                self.isEditing = false;
                self.skipProjectCascade = false;   // re-enable cascade after a possibly-aborted edit
                $('#contractor-form')[0].reset();

                // Employee ID is auto-generated on save - hide the field for new contractors
                $('#employee_id').val('');
                $('#employee_id_field').hide();

                // Reset provider; the picker depends on provider, so start it empty
                // (the cascade adds the first row once a provider is chosen)
                $('#provider_code').val('');
                $('#provider_code').prop('disabled', false);
                self.availableProjects = [];
                self.resetProjectRows();

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

                // Store current provider + per-project assignments for the edit modal
                // (provider locked; rows pre-filled from the exact project_positions map)
                let assignments = [];
                try {
                    const map = JSON.parse(contractor.project_positions || '{}');
                    assignments = Object.keys(map).map(function(code) {
                        return { project_code: code, position: map[code] || '' };
                    });
                } catch (e) {
                    // Unparseable map: fall back to the codes CSV with blank positions
                    assignments = (contractor.project_codes || '')
                        .split(',').map(function(c) { return c.trim(); }).filter(Boolean)
                        .map(function(code) { return { project_code: code, position: '' }; });
                }
                self.currentEditingAssignments = assignments;
                self.currentEditingProvider = contractor.provider_code;

                // Employee ID is auto-generated and read-only
                $('#employee_id').val(contractor.employee_id);
                $('#employee_id').prop('readonly', true);
                $('#employee_id_field').show();
                $('#name').val(contractor.name);
                $('#gender').val(contractor.gender);
                $('#birthdate').val(self.formatDateForInput(contractor.birthdate));
                $('#contact_number').val(contractor.contact_number);
                $('#address').val(contractor.address || '');
                $('#provider_code').val(contractor.provider_code);

                // Set toggle state based on contractor status
                $('#contractor_active').prop('checked', contractor.active === 1);
                $('#contractor_active_label').text(contractor.active === 1 ? 'Active' : 'Inactive');

                $('#contractor-modal .modal-title').text('Edit Contractor');

                // Use Bootstrap 5 native API
                var modal = new bootstrap.Modal(document.getElementById('contractor-modal'));
                modal.show();
            });

            // Row checkbox: track selection (survives pagination/filters).
            $('#contractors-table tbody').on('change', '.contractor-select', function() {
                const id = String($(this).data('employee-id'));
                if (this.checked) { self.selectedContractorIds.add(id); }
                else { self.selectedContractorIds.delete(id); }
                self._syncContractorCheckboxState();
            });

            // Header select-all: applies to the CURRENT PAGE only. The count on the
            // Delete Selected button always reflects the full selection (all pages).
            $('#select-all-contractors').on('change', function() {
                const checked = this.checked;
                self.contractorTable.rows({ page: 'current' }).every(function() {
                    const id = String(this.data().employee_id);
                    $(this.node()).find('.contractor-select').prop('checked', checked);
                    if (checked) { self.selectedContractorIds.add(id); } else { self.selectedContractorIds.delete(id); }
                });
                self._updateDeleteSelectedButton();
            });

            // Per-row delete (soft delete: attendance history kept, kiosk scans rejected afterwards)
            $(document).on('click', '.btn-delete-contractor', function() {
                const employeeId = String($(this).data('employee-id'));
                const contractor = self.contractorTable.row($(this).closest('tr')).data();
                const name = (contractor && contractor.name) ? contractor.name : employeeId;

                AdminPage.common.showConfirmation(
                    'Are you sure you want to delete contractor ' + name + ' (' + employeeId + ')? ',
                    function() {
                        $.ajax({
                            url: '/Admin/DeleteContractors',
                            method: 'POST',
                            contentType: 'application/json',
                            data: JSON.stringify({ employee_ids: [employeeId] }),
                            success: function(response) {
                                if (response.success) {
                                    AdminPage.common.showSuccess(response.message);
                                    self.selectedContractorIds.delete(employeeId);
                                    self._updateDeleteSelectedButton();
                                    self.contractorTable.ajax.reload(function() {
                                        self._populateProjectFilter();   // a project may now have zero rows
                                    });
                                } else {
                                    AdminPage.common.showError(response.message);
                                }
                            },
                            error: function(xhr, status, error) {
                                AdminPage.common.showError('Failed to delete contractor. Please try again.');
                                console.error('Delete contractor error:', { xhr, status, error });
                            }
                        });
                    }
                );
            });

            // Bulk delete of every checked contractor (across pages/filters)
            $('#delete-selected-contractors').on('click', function() {
                const ids = Array.from(self.selectedContractorIds);
                if (ids.length === 0) {
                    AdminPage.common.showWarning('Select at least one contractor first.');
                    return;
                }

                AdminPage.common.showConfirmation(
                    'Are you sure you want to delete ' + ids.length + ' selected contractor(s)?',
                    function() {
                        $.ajax({
                            url: '/Admin/DeleteContractors',
                            method: 'POST',
                            contentType: 'application/json',
                            data: JSON.stringify({ employee_ids: ids }),
                            success: function(response) {
                                if (response.success) {
                                    AdminPage.common.showSuccess(response.message);
                                    self.selectedContractorIds.clear();
                                    self._updateDeleteSelectedButton();
                                    self.contractorTable.ajax.reload(function() {
                                        self._populateProjectFilter();
                                    });
                                } else {
                                    AdminPage.common.showError(response.message);
                                }
                            },
                            error: function(xhr, status, error) {
                                AdminPage.common.showError('Failed to delete contractors. Please try again.');
                                console.error('Bulk delete contractors error:', { xhr, status, error });
                            }
                        });
                    }
                );
            });

            // Toggle state change handler
            $('#contractor_active').on('change', function() {
                const isActive = $(this).is(':checked');
                $('#contractor_active_label').text(isActive ? 'Active' : 'Inactive');
            });

            // Provider change -> rebuild the picker from the new provider's projects (cascade)
            $('#provider_code').on('change', function() {
                // Skip the cascade while we programmatically set the provider during edit init
                if (self.skipProjectCascade) return;
                const providerCode = $(this).val();
                if (providerCode) {
                    self.fetchProjectsForProvider(providerCode, function() {
                        self.resetProjectRows();
                        self.addProjectRow(null, '');
                    });
                } else {
                    // No provider selected: clear the picker and disable the Add button
                    self.availableProjects = [];
                    self.resetProjectRows();
                    $('#add-project-row').prop('disabled', true);
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

                // Add-row picker: every row needs a project and a position, no duplicates
                const assignments = self.collectAssignments();
                if (assignments.length === 0) {
                    AdminPage.common.showError('At least one project must be assigned');
                    return;
                }
                if (assignments.some(a => !a.project_code)) {
                    AdminPage.common.showError('Select a project for every row');
                    return;
                }
                if (assignments.some(a => !a.position)) {
                    AdminPage.common.showError('Position is required');
                    return;
                }
                const codes = assignments.map(a => a.project_code);
                if (codes.some((c, i) => codes.indexOf(c) !== i)) {
                    AdminPage.common.showError('Each project can only be assigned once');
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
                    address: $('#address').val(),
                    provider_code: providerCode,
                    // Per-project assignments: [{project_code, position}] — the server
                    // binds List<contractor_project_assignment> and serializes p_projects
                    assignments: assignments,
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

        // ---- Add-row picker (per-project position) --------------------------------
        //
        // #contractor-projects-container is the <tbody> of the Project|Position
        // table and holds one .contractor-assignment-row <tr> per assignment:
        // a Select2 project dropdown | a position input | a remove button. Each
        // row's dropdown offers the provider's non-expired projects minus the
        // projects already chosen in OTHER rows.

        // Cache the provider's projects (ALL of them, expired flagged) for the
        // picker rows; expired ones stay offerable only where already assigned.
        fetchProjectsForProvider: function(providerCode, onComplete) {
            const self = this;
            $.ajax({
                url: '/Admin/GetProjectsByProvider',
                method: 'GET',
                data: { provider_code: providerCode },
                success: function(response) {
                    if (response.success && response.data) {
                        const today = new Date();
                        today.setHours(0, 0, 0, 0);
                        self.availableProjects = response.data.map(function(project) {
                            // Expired = contract_enddate in the past; null enddate =
                            // open-ended contract (not expired).
                            const isExpired = !!(project.contract_enddate && new Date(project.contract_enddate) < today);
                            return {
                                project_code: project.project_code,
                                project_name: project.project_name,
                                isExpired: isExpired
                            };
                        });
                        $('#add-project-row').prop('disabled', false);
                    } else {
                        self.availableProjects = [];
                    }
                    if (onComplete) onComplete();
                },
                error: function(xhr, status, error) {
                    console.error('Failed to load projects for provider:', { xhr, status, error });
                    AdminPage.common.showError('Failed to load projects. Please try again.');
                    self.availableProjects = [];
                    if (onComplete) onComplete();
                }
            });
        },

        // Codes currently picked in some row (for the other rows' exclusions and
        // the expired-but-assigned allowance)
        pickedProjectCodes: function() {
            return $('#contractor-projects-container .cpa-project')
                .map(function() { return $(this).val() || null; })
                .get()
                .filter(Boolean);
        },

        // Append one assignment row (optionally pre-selected + pre-filled) and
        // rebuild every row's option list. Rows are <tr>s of the Project|Position
        // table: select | position input | remove button.
        addProjectRow: function(selectedCode, positionText) {
            const self = this;
            const $row = $(
                '<tr class="contractor-assignment-row">' +
                    '<td><select class="form-control cpa-project"></select></td>' +
                    '<td><input type="text" class="form-control cpa-position" placeholder="Position on this project" maxlength="255"></td>' +
                    '<td class="text-center"><button type="button" class="btn btn-sm btn-outline-danger cpa-remove" title="Remove this project"><i class="fas fa-times"></i></button></td>' +
                '</tr>'
            );
            $('#contractor-projects-container').append($row);

            const $select = $row.find('.cpa-project');
            $select.select2({
                placeholder: 'Select Project',
                allowClear: false,
                width: '100%',
                dropdownParent: $('#contractor-modal')
            });
            $row.find('.cpa-position').val(positionText || '');

            self.rebuildRowOptions($row, selectedCode);
        },

        // Destroy the row's Select2 BEFORE removing it (its dropdown lives in the
        // modal body and would be orphaned otherwise), then refresh the others.
        removeProjectRow: function($row) {
            const $select = $row.find('.cpa-project');
            if ($select.data('select2')) {
                $select.select2('destroy');
            }
            $row.remove();
            this.rebuildRowOptions();
        },

        // Rebuild each row's <option>s: provider's projects minus the projects
        // chosen in other rows; expired projects offered only when already picked
        // somewhere (edit mode keeps expired-but-assigned projects selectable).
        // Pass $focusRow + selectedCode to set a value into a freshly added row.
        rebuildRowOptions: function($focusRow, selectedCode) {
            const self = this;
            const picked = self.pickedProjectCodes();

            $('#contractor-projects-container .contractor-assignment-row').each(function() {
                const $row = $(this);
                const $select = $row.find('.cpa-project');
                const currentValue = ($row.is($focusRow) && selectedCode) ? selectedCode : $select.val();

                // Projects offered to THIS row: everything except what OTHER rows took
                const takenByOthers = picked.filter(function(code) { return code !== currentValue; });

                $select.empty().append('<option value=""></option>');
                self.availableProjects.forEach(function(project) {
                    if (takenByOthers.indexOf(project.project_code) !== -1) return;
                    // Skip expired projects unless the assignment already exists in
                    // some row — matches the old edit-mode behavior.
                    const isAssignedSomewhere = picked.indexOf(project.project_code) !== -1;
                    if (project.isExpired && !isAssignedSomewhere) return;
                    $select.append('<option value="' + project.project_code + '">' +
                        project.project_name + ' (' + project.project_code + ')</option>');
                });

                if (currentValue) {
                    // Keep a value that is no longer among the options (e.g., an
                    // expired assignment) by re-adding it explicitly.
                    if ($select.find('option[value="' + currentValue + '"]').length === 0) {
                        $select.append('<option value="' + currentValue + '">' + currentValue + '</option>');
                    }
                    $select.val(currentValue);
                } else {
                    $select.val(null);
                }
                $select.trigger('change.select2');
            });

            // Hide remove buttons when only one row remains (>= 1 assignment required)
            const $rows = $('#contractor-projects-container .contractor-assignment-row');
            $rows.find('.cpa-remove').toggle($rows.length > 1);
        },

        // Read the picker rows into [{project_code, position}] (trimmed)
        collectAssignments: function() {
            return $('#contractor-projects-container .contractor-assignment-row')
                .map(function() {
                    const $row = $(this);
                    return {
                        project_code: ($row.find('.cpa-project').val() || '').trim(),
                        position: ($row.find('.cpa-position').val() || '').trim()
                    };
                })
                .get();
        },

        // Tear down every row (Select2 destroy + empty the container)
        resetProjectRows: function() {
            $('#contractor-projects-container .contractor-assignment-row').each(function() {
                const $select = $(this).find('.cpa-project');
                if ($select.data('select2')) {
                    $select.select2('destroy');
                }
            });
            $('#contractor-projects-container').empty();
            $('#add-project-row').prop('disabled', false);
        },

        // (loadProjectsByProvider was removed: the add-row picker helpers above
        // replaced it — fetchProjectsForProvider + rebuildRowOptions.)

        loadProviders: function(selectedProviderCode = null, onComplete = null) {
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
                },
                complete: function() {
                    if (onComplete) onComplete();
                }
            });
        },

        formatDateForInput: function(dateString) {
            if (!dateString) return '';
            return dateString.split('T')[0];
        },

        exportToExcel: async function() {
            // Export only the rows currently shown (respects global search + Status/Project filters)
            const tableData = this.contractorTable.rows({ search: 'applied' }).data().toArray();

            if (tableData.length === 0) {
                AdminPage.common.showWarning('No data available to export', 'No Data');
                return;
            }

            Swal.fire({ title: 'Generating Excel...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });

            try {
                // The QR PNGs are generated once at enrollment and stored on
                // contractor_employee (contractor_employee.qr_code_image) — the export
                // only fetches and embeds them, it never generates.
                const qrRes = await fetch('/Admin/GetContractorQrCodes');
                const qrJson = await qrRes.json();
                const qrMap = {};
                (qrJson.success && qrJson.data ? qrJson.data : []).forEach(q => { qrMap[q.employee_id] = q.qr_code_image; });

                // ExcelJS (not SheetJS) because community SheetJS cannot embed images.
                const wb = new ExcelJS.Workbook();
                const ws = wb.addWorksheet('Contractors', { views: [{ state: 'frozen', ySplit: 1 }] });
                ws.columns = [
                    { header: 'Employee ID', key: 'employee_id', width: 14 },
                    { header: 'Name', key: 'name', width: 30 },
                    { header: 'Provider Name', key: 'provider_name', width: 25 },
                    { header: 'Project (Position - Area)', key: 'project_details', width: 55 },
                    { header: 'Status', key: 'status', width: 10 },
                    { header: 'QR Code', key: 'qr', width: 16 }
                ];
                ws.getRow(1).font = { bold: true };
                // Wrap text set here so the LF-joined "Name (Position - Area)" lines
                // stack without the user enabling Wrap Text in Excel (community SheetJS
                // could not do this — a bonus of the ExcelJS switch).
                ws.getColumn(4).alignment = { wrapText: true, vertical: 'top' };

                tableData.forEach(row => {
                    const added = ws.addRow({
                        employee_id: row.employee_id,
                        name: row.name,
                        provider_name: row.provider_name || '',
                        project_details: row.project_details || row.project_names || row.project_codes || '',
                        status: row.active === 1 ? 'Active' : 'Inactive'
                    });
                    const r = added.number;  // 1-based worksheet row (header = 1)

                    // qr_code_image arrives as a raw base64 string (System.Text.Json
                    // byte[] encoding — no "data:" prefix), exactly what addImage wants.
                    const base64 = qrMap[row.employee_id];
                    if (base64) {
                        const imgId = wb.addImage({ base64: base64, extension: 'png' });
                        // tl is 0-indexed INCLUDING the header → first data row = tl.row 1.
                        ws.addImage(imgId, { tl: { col: 5, row: r - 1 }, ext: { width: 64, height: 64 } });
                        // Row height is POINTS: 64px * 3/4 = 48pt + 2pt breathing room.
                        ws.getRow(r).height = 50;
                    }
                });

                const buffer = await wb.xlsx.writeBuffer();
                const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, '-').replace('T', '_');
                const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
                // ExcelJS has no XLSX.writeFile equivalent — anchor-click download.
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `Contractors_${timestamp}.xlsx`;
                document.body.appendChild(a);
                a.click();
                a.remove();
                URL.revokeObjectURL(url);

                Swal.close();
                AdminPage.common.showSuccess(`Exported ${tableData.length} contractors to Excel`, 'Export Successful');
            } catch (err) {
                console.error('Contractors Excel export failed:', err);
                Swal.close();
                AdminPage.common.showError('Could not generate the Excel file.');
            }
        },

        // ===== Bulk Import Contractors =====
        parsedBulkRows: [],          // validated rows ready to submit
        _currentBulkFileName: '',

        // Example rows embedded in the downloadable template. If an uploaded file still
        // contains them, they are auto-detected and skipped (never inserted). Shared by
        // downloadBulkTemplate and the skip-detector so the two never drift apart.
        _bulkSampleRows: [
            { name: 'Juan Dela Cruz', gender: 'Male', birthdate: '1990-01-15', position: 'Welder', contact_number: '09171234567', address: 'Quezon City' },
            { name: 'Maria Santos', gender: 'Female', birthdate: '1995-07-22', position: 'Admin Clerk', contact_number: '', address: '' }
        ],

        // Normalized signature of a row's 6 user fields. Matching all fields makes a
        // real-contractor collision effectively impossible. (Area of Destination is
        // no longer a bulk column — it comes from the selected project — so legacy
        // templates that still carry an Area column still match the samples.)
        _bulkRowSignature: function(row) {
            return [
                (row.name || ''), (row.gender || ''), (row.birthdate || ''),
                (row.position || ''), (row.contact_number || ''), (row.address || '')
            ].map(function(v) { return String(v).trim().toLowerCase(); }).join('|');
        },

        // True when a parsed row matches one of the template samples exactly (all fields).
        _isBulkSampleRow: function(row) {
            const sig = this._bulkRowSignature(row);
            const self = this;
            return this._bulkSampleRows.some(function(s) { return self._bulkRowSignature(s) === sig; });
        },

        // Duplicate key for a contractor: name (case-insensitive) + birthdate (yyyy-mm-dd).
        // Matches the server-side rule (sp_contractor_employee_CheckDuplicate).
        _bulkDuplicateKey: function(name, birthdate) {
            const n = String(name || '').trim().toLowerCase();
            const b = String(birthdate || '').split('T')[0];   // JSON birthdate may be ISO "...T00:00:00"
            return n + '|' + b;
        },

        // Build the set of already-enrolled contractor keys for the selected provider from
        // the loaded contractors DataTable (active + inactive, per the duplicate policy —
        // duplicates are provider-scoped since employee IDs and duplicate checks are).
        // No new endpoint — reuses the data behind /Admin/GetAllContractors.
        _buildBulkDuplicateKeys: function() {
            const keys = new Set();
            const providerCode = $('#bulk_provider_code').val();
            if (!providerCode || !this.contractorTable) return keys;
            this.contractorTable.data().each(function(row) {
                if (!row) return;
                if (row.provider_code !== providerCode) return;
                const key = AdminPage.contractors._bulkDuplicateKey(row.name, row.birthdate);
                if (key && key !== '|') keys.add(key);
            });
            return keys;
        },

        // Flag duplicate rows in the preview: against existing project contractors AND
        // within the file itself. Idempotent — clears prior duplicate flags first so it
        // can be re-run when the selected project changes. A row matching an already-
        // enrolled contractor (same provider) is NOT an error: the server merges it into
        // the existing account, so it gets a non-blocking row.notice (amber badge). Only
        // duplicates WITHIN the file stay blocking errors (row.error + row.duplicate).
        _flagBulkDuplicates: function() {
            const self = this;
            // Reset previously-flagged duplicates (leave field errors intact).
            this.parsedBulkRows.forEach(function(r) {
                if (r.duplicate) { r.error = null; r.notice = null; r.duplicate = false; }
            });
            const dbKeys = this._buildBulkDuplicateKeys();
            const batchSeen = {};
            this.parsedBulkRows.forEach(function(r) {
                if (r.skipped || r.error) return;   // only check otherwise-valid rows
                const key = self._bulkDuplicateKey(r.name, r.birthdate);
                if (!key || key === '|') return;
                if (dbKeys.has(key)) {
                    r.notice = 'Already enrolled — will update the existing account';
                    r.duplicate = true;
                    batchSeen[key] = true;
                } else if (batchSeen[key]) {
                    r.error = 'Duplicate of another row in this file';
                    r.duplicate = true;
                } else {
                    batchSeen[key] = true;
                }
            });
        },

        initBulkImport: function() {
            const self = this;

            // Open the bulk-import modal
            $('#bulk-import').on('click', function() {
                self.openBulkImportModal();
            });

            // Select2 on the bulk project select, scoped to the bulk modal
            $('#bulk-import-modal').on('shown.bs.modal', function() {
                if (!$('#bulk_project_code').data('select2')) {
                    $('#bulk_project_code').select2({
                        placeholder: 'Select Project',
                        allowClear: true,
                        width: '100%',
                        dropdownParent: $('#bulk-import-modal')
                    });
                }
                self.loadBulkProviders();
                $(window).trigger('resize');
            });

            // Reset everything when the modal closes
            $('#bulk-import-modal').on('hidden.bs.modal', function() {
                $('#bulk_file').val('');
                $('#bulk_provider_code').empty().append('<option value="">Select Provider</option>');
                $('#bulk_project_code').empty().append('<option value="">Select Provider first</option>')
                    .val(null).trigger('change.select2').prop('disabled', true);
                $('#bulk-preview-wrap').hide();
                $('#bulk-preview tbody').empty();
                $('#process-bulk-import').prop('disabled', true);
                self.parsedBulkRows = [];
            });

            // Provider -> Project cascade (bulk selects). The duplicate set is
            // provider-scoped, so re-run duplicate detection (and re-render) whenever
            // the provider changes and a file is already loaded.
            $('#bulk_provider_code').on('change', function() {
                const providerCode = $(this).val();
                if (providerCode) {
                    self.loadBulkProjectsByProvider(providerCode);
                } else {
                    $('#bulk_project_code').empty().append('<option value="">Select Provider first</option>')
                        .val(null).trigger('change.select2').prop('disabled', true);
                }
                if (self.parsedBulkRows.length) {
                    self._flagBulkDuplicates();
                    self.renderBulkPreview();
                }
            });

            // Download Excel template
            $('#bulk-download-template').on('click', function() {
                self.downloadBulkTemplate();
            });

            // File chosen -> parse + preview
            $('#bulk_file').on('change', function(e) {
                const file = e.target.files && e.target.files[0];
                if (file) {
                    self.handleBulkFile(file);
                }
            });

            // Process the import
            $('#process-bulk-import').on('click', function() {
                self.processBulkImport();
            });

            // Delete a row directly in the preview (delegated so it survives re-renders).
            // Uses the array index encoded in data-index.
            $('#bulk-preview tbody').on('click', '.bulk-row-delete', function() {
                const idx = parseInt($(this).data('index'), 10);
                if (!isNaN(idx)) {
                    self.parsedBulkRows.splice(idx, 1);
                    self.renderBulkPreview();
                }
            });
        },

        openBulkImportModal: function() {
            this.parsedBulkRows = [];
            this._currentBulkFileName = '';
            $('#bulk_file').val('');
            $('#bulk-preview-wrap').hide();
            $('#bulk-preview tbody').empty();
            $('#bulk-preview-summary').text('');
            $('#process-bulk-import').prop('disabled', true);
            $('#bulk_project_code').empty().append('<option value="">Select Provider first</option>')
                .val(null).trigger('change.select2').prop('disabled', true);

            var modal = new bootstrap.Modal(document.getElementById('bulk-import-modal'));
            modal.show();
        },

        loadBulkProviders: function() {
            const $dropdown = $('#bulk_provider_code');
            $dropdown.empty().append('<option value="">Select Provider</option>');

            $.ajax({
                url: '/Admin/GetAllProviders',
                method: 'GET',
                success: function(response) {
                    if (response.success && response.data) {
                        response.data.forEach(function(provider) {
                            $dropdown.append('<option value="' + provider.provider_code + '">' +
                                provider.provider_name + ' (' + provider.provider_code + ')</option>');
                        });
                    }
                },
                error: function() {
                    AdminPage.common.showError('Failed to load providers. Please try again.');
                }
            });
        },

        loadBulkProjectsByProvider: function(providerCode) {
            const $dropdown = $('#bulk_project_code');
            $dropdown.prop('disabled', false);
            $dropdown.empty().append('<option value="">Select Project</option>');

            $.ajax({
                url: '/Admin/GetProjectsByProvider',
                method: 'GET',
                data: { provider_code: providerCode },
                success: function(response) {
                    if (response.success && response.data) {
                        const today = new Date(); today.setHours(0, 0, 0, 0);
                        response.data.forEach(function(project) {
                            // Hide expired projects for new enrollment
                            const isExpired = project.contract_enddate && new Date(project.contract_enddate) < today;
                            if (isExpired) return;
                            $dropdown.append('<option value="' + project.project_code + '">' +
                                project.project_name + ' (' + project.project_code + ')</option>');
                        });
                    }
                    $dropdown.val(null).trigger('change.select2');
                },
                error: function() {
                    AdminPage.common.showError('Failed to load projects. Please try again.');
                }
            });
        },

        downloadBulkTemplate: function() {
            const ws = XLSX.utils.json_to_sheet(this._bulkSampleRows);
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, 'Contractors');
            XLSX.writeFile(wb, 'BulkContractorTemplate.xlsx');
        },

        handleBulkFile: function(file) {
            const self = this;
            const reader = new FileReader();
            const isCsv = /\.csv$/i.test(file.name);

            if (isCsv) {
                reader.onload = function(e) {
                    try {
                        const wb = XLSX.read(e.target.result, { type: 'string', raw: false });
                        self._parseBulkWorkbook(wb, file.name);
                    } catch (err) {
                        console.error(err);
                        AdminPage.common.showError('Could not read the CSV file.');
                    }
                };
                reader.readAsText(file);
            } else {
                reader.onload = function(e) {
                    try {
                        const wb = XLSX.read(new Uint8Array(e.target.result), { type: 'array', cellDates: true, raw: false });
                        self._parseBulkWorkbook(wb, file.name);
                    } catch (err) {
                        console.error(err);
                        AdminPage.common.showError('Could not read the Excel file.');
                    }
                };
                reader.readAsArrayBuffer(file);
            }
        },

        _parseBulkWorkbook: function(wb, fileName) {
            const self = this;
            const ws = wb.Sheets[wb.SheetNames[0]];
            if (!ws) {
                AdminPage.common.showError('The file has no sheets.');
                return;
            }
            // raw:false -> formatted strings (handles Excel date cells); defval:'' keeps blanks.
            const rows = XLSX.utils.sheet_to_json(ws, { raw: false, defval: '' });
            this._currentBulkFileName = fileName;
            this.parsedBulkRows = rows.map(function(raw, idx) {
                return self._mapBulkRow(raw, idx + 2);   // +2: header row + 1-based numbering
            }).map(function(row) {
                // Auto-skip template sample rows the admin forgot to remove. They stay in
                // the preview (shown as skipped) but are never sent to the server.
                if (self._isBulkSampleRow(row)) {
                    row.skipped = true;
                    row.error = null;
                }
                return row;
            });
            // Flag duplicates (against existing project contractors + within the file).
            this._flagBulkDuplicates();
            this.renderBulkPreview();
        },

        _mapBulkRow: function(raw, rowNumber) {
            // Match headers leniently (case/space/underscore insensitive)
            const get = function(key) {
                const norm = key.replace(/[\s_]/g, '').toLowerCase();
                const keys = Object.keys(raw);
                for (let i = 0; i < keys.length; i++) {
                    if (keys[i].replace(/[\s_]/g, '').toLowerCase() === norm) {
                        const v = raw[keys[i]];
                        return (v === null || v === undefined) ? '' : String(v).trim();
                    }
                }
                return '';
            };

            const genderRaw = get('gender');
            const g = genderRaw.toLowerCase();
            let gender = '';
            if (g === 'male' || g === 'm') gender = 'Male';
            else if (g === 'female' || g === 'f') gender = 'Female';

            const birthdateRaw = get('birthdate');

            const row = {
                row_number: rowNumber,
                name: get('name'),
                gender: gender,
                birthdate: birthdateRaw,
                contact_number: get('contact_number') || get('contact'),
                address: get('address'),
                position: get('position'),
                error: null
            };

            // Light client-side validation. The server is still authoritative, but this
            // gives the admin an instant preview and avoids sending obviously bad rows.
            const dob = birthdateRaw ? new Date(birthdateRaw) : null;
            const validDob = dob && !isNaN(dob.getTime());

            if (!row.name) row.error = 'Name is required';
            else if (!gender) row.error = 'Gender must be Male or Female';
            else if (!birthdateRaw) row.error = 'Birthdate is required';
            else if (!validDob) row.error = 'Birthdate is not a valid date';
            else if (!row.position) row.error = 'Position is required';
            else {
                // DOLE 18+ pre-check (mirrors ContractorService.ValidateBirthdate)
                const d = new Date(dob); d.setHours(0, 0, 0, 0);
                const today = new Date(); today.setHours(0, 0, 0, 0);
                if (d > today) row.error = 'Birthdate cannot be a future date';
                else {
                    let age = today.getFullYear() - d.getFullYear();
                    const m = today.getMonth() - d.getMonth();
                    if (m < 0 || (m === 0 && today.getDate() < d.getDate())) age--;
                    if (age < 18) row.error = 'Under 18 — not eligible (DOLE)';
                }
            }

            // Normalize birthdate to YYYY-MM-DD for the server
            if (!row.error && validDob) {
                row.birthdate = dob.getFullYear() + '-' +
                    String(dob.getMonth() + 1).padStart(2, '0') + '-' +
                    String(dob.getDate()).padStart(2, '0');
            }

            return row;
        },

        renderBulkPreview: function() {
            const $tbody = $('#bulk-preview tbody');
            $tbody.empty();

            const valid = this.parsedBulkRows.filter(function(r) { return !r.error && !r.skipped; });
            const invalid = this.parsedBulkRows.filter(function(r) { return r.error; });
            const skipped = this.parsedBulkRows.filter(function(r) { return r.skipped; });

            this.parsedBulkRows.forEach(function(r, idx) {
                let badge;
                if (r.skipped) {
                    badge = '<span class="badge bg-secondary">Sample — skipped</span>';
                } else if (r.error) {
                    badge = '<span class="badge bg-danger">' + r.error + '</span>';
                } else if (r.notice) {
                    badge = '<span class="badge bg-warning text-dark">' + r.notice + '</span>';
                } else {
                    badge = '<span class="badge bg-success">OK</span>';
                }
                $tbody.append(
                    '<tr>' +
                    '<td>' + r.row_number + '</td>' +
                    '<td>' + (r.name || '') + '</td>' +
                    '<td>' + (r.gender || '') + '</td>' +
                    '<td>' + (r.birthdate || '') + '</td>' +
                    '<td>' + (r.position || '') + '</td>' +
                    '<td>' + badge + '</td>' +
                    '<td class="text-center">' +
                        '<button type="button" class="btn btn-sm btn-link text-danger p-0 bulk-row-delete" data-index="' + idx + '" title="Remove row">' +
                            '<i class="fas fa-trash"></i>' +
                        '</button>' +
                    '</td>' +
                    '</tr>'
                );
            });

            // Total count at the top of the preview, with a valid/error/skipped breakdown.
            const updating = this.parsedBulkRows.filter(function(r) { return r.notice && !r.error && !r.skipped; });
            const parts = [valid.length + ' valid'];
            if (updating.length) parts.push('<span class="text-warning">' + updating.length + ' will update existing</span>');
            if (invalid.length) parts.push('<span class="text-danger">' + invalid.length + ' with errors</span>');
            if (skipped.length) parts.push('<span class="text-secondary">' + skipped.length + ' sample skipped</span>');
            $('#bulk-preview-summary').html(
                '<strong>Total rows: ' + this.parsedBulkRows.length + '</strong>' +
                ' <span class="text-muted">(' + parts.join(', ') + ')</span>'
            );

            $('#bulk-preview-wrap').show();
            // Block processing while any row still has an error.
            $('#process-bulk-import').prop('disabled', !(valid.length > 0 && invalid.length === 0));

            if (this.parsedBulkRows.length === 0) {
                AdminPage.common.showWarning('No rows found in the file.');
            }
        },

        // Rebuild the #filter-project dropdown from the currently-loaded table data.
        // Idempotent: clears previously appended options first and preserves the
        // current selection when that project still exists. Used by initComplete
        // and after a bulk-import ajax.reload() (initComplete only runs once, so
        // the dropdown would otherwise stay stale for a newly imported project).
        _populateProjectFilter: function() {
            const self = this;
            const $sel = $('#filter-project');
            const prevVal = $sel.val();   // preserve current selection if still valid

            // Keep only the static "All" placeholder; drop previously appended options.
            $sel.find('option').not('[value=""]').remove();

            const projects = {};
            self.contractorTable.data().each(function(row) {
                // Multi-project: split the CSVs and register each unique code->name
                // pair (a project shared by many contractors appears once).
                const codes = String(row.project_codes || '').split(',').map(function(c) { return c.trim(); }).filter(Boolean);
                const names = String(row.project_names || '').split(',').map(function(n) { return n.trim(); });
                codes.forEach(function(code, i) {
                    if (!projects[code]) {
                        projects[code] = names[i] || code;
                    }
                });
            });
            Object.keys(projects).sort().forEach(function(code) {
                $sel.append($('<option></option>').val(code).text(projects[code] + ' (' + code + ')'));
            });

            // Restore previous selection if that project still exists, else "All".
            if (prevVal && $sel.find('option[value="' + prevVal + '"]').length) {
                $sel.val(prevVal);
            } else {
                $sel.val('');
            }
        },

        // Re-apply checkbox state to the visible (current page) rows from the
        // selection Set, then refresh the header select-all and the Delete
        // Selected button. Runs on every draw so selection survives paging.
        _syncContractorCheckboxState: function() {
            const self = this;
            if (!this.contractorTable) return;
            let visible = 0, selected = 0;
            this.contractorTable.rows({ page: 'current' }).every(function() {
                visible++;
                const isChecked = self.selectedContractorIds.has(String(this.data().employee_id));
                $(this.node()).find('.contractor-select').prop('checked', isChecked);
                if (isChecked) selected++;
            });
            // Header reflects the CURRENT PAGE only; the button count reflects the full selection.
            $('#select-all-contractors').prop('checked', visible > 0 && visible === selected);
            this._updateDeleteSelectedButton();
        },

        // Enable/disable the bulk Delete Selected button and show the selection count.
        _updateDeleteSelectedButton: function() {
            const n = this.selectedContractorIds.size;
            $('#delete-selected-contractors').prop('disabled', n === 0);
            $('#selected-contractor-count').text(n > 0 ? ' (' + n + ')' : '');
        },

        processBulkImport: function() {
            const self = this;
            const providerCode = $('#bulk_provider_code').val();
            const projectCode = $('#bulk_project_code').val();

            if (!providerCode) { AdminPage.common.showError('Please select a Provider.'); return; }
            if (!projectCode) { AdminPage.common.showError('Please select a Project.'); return; }

            // Hard block: the preview must have zero error rows before importing.
            const invalidRows = this.parsedBulkRows.filter(function(r) { return r.error; });
            if (invalidRows.length > 0) {
                AdminPage.common.showError('Please fix or remove all rows with errors before importing (' + invalidRows.length + ' remaining).');
                return;
            }

            // Skipped template samples are never sent.
            const validRows = this.parsedBulkRows.filter(function(r) { return !r.error && !r.skipped; });
            if (validRows.length === 0) {
                AdminPage.common.showError('There are no valid rows to import.');
                return;
            }

            const payload = {
                provider_code: providerCode,
                project_code: projectCode,
                file_name: this._currentBulkFileName || '',
                contractors: validRows.map(function(r) {
                    return {
                        row_number: r.row_number,
                        name: r.name,
                        gender: r.gender,
                        birthdate: r.birthdate,
                        contact_number: r.contact_number,
                        address: r.address,
                        position: r.position
                    };
                })
            };

            const $btn = $('#process-bulk-import').prop('disabled', true)
                .html('<span class="spinner-border spinner-border-sm"></span> Processing...');

            $.ajax({
                url: '/Admin/BulkCreateContractors',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload),
                success: function(response) {
                    $btn.prop('disabled', false).html('<i class="fas fa-check"></i> Process Import');
                    if (response.success && response.data) {
                        self._showBulkResult(response.data);
                        // Reload, then rebuild the Project filter dropdown so a newly
                        // imported project (one that had zero contractors before) appears.
                        self.contractorTable.ajax.reload(function() {
                            self._populateProjectFilter();
                        });
                    } else {
                        AdminPage.common.showError(response.message || 'Bulk import failed.');
                    }
                },
                error: function() {
                    $btn.prop('disabled', false).html('<i class="fas fa-check"></i> Process Import');
                    AdminPage.common.showError('Failed to process the import. Please try again.');
                }
            });
        },

        _showBulkResult: function(data) {
            const self = this;
            const errorRows = (data.errors && data.errors.length)
                ? '<hr><div class="text-start"><strong>Failed rows (' + data.error_count + '):</strong>' +
                  '<table class="table table-sm table-bordered mt-2 mb-0"><thead><tr>' +
                  '<th>Row</th><th>Name</th><th>Reason</th></tr></thead><tbody>' +
                  data.errors.map(function(e) {
                      return '<tr><td>' + e.row + '</td><td>' + (e.name || '') + '</td><td>' + e.message + '</td></tr>';
                  }).join('') +
                  '</tbody></table></div>'
                : '';

            // Hide the import modal so the result popup is not trapped behind it
            const modalEl = document.getElementById('bulk-import-modal');
            const modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();

            // Merged rows (duplicates that updated an existing account) are reported
            // separately from the newly created ones. Older batches have no
            // merged_count field — default it.
            const mergedCount = data.merged_count || 0;
            const newCount = (data.success_count || 0) - mergedCount;
            const mergedSuffix = mergedCount > 0 ? ' (+ ' + mergedCount + ' updated existing)' : '';

            Swal.fire({
                icon: data.error_count > 0 ? 'warning' : 'success',
                title: 'Bulk Import Complete',
                html: '<div>Successfully enrolled: <strong>' + newCount + ' new' + mergedSuffix + '</strong> of ' +
                      data.total + '</div>' +
                      (data.enrolled_employee_ids && data.enrolled_employee_ids.length
                          ? '<div class="text-muted small mt-1">New IDs: ' + data.enrolled_employee_ids.join(', ') + '</div>'
                          : '') +
                      (mergedCount > 0 && data.merged_employee_ids && data.merged_employee_ids.length
                          ? '<div class="text-muted small mt-1">Updated existing IDs: ' + data.merged_employee_ids.join(', ') + '</div>'
                          : '') +
                      errorRows,
                confirmButtonText: 'OK',
                width: data.error_count > 0 ? '640px' : undefined
            }).then(function() {
                self.parsedBulkRows = [];
            });
        }
    },

    // AuditLogs.cshtml - Audit log viewer
    auditLogs: {
        auditTable: null,

        init: function() {
            const self = this;

            self.auditTable = $('#audit-logs-table').DataTable({
                ajax: {
                    url: '/Admin/GetAllAuditLogs',
                    dataSrc: function(data) { return data.success ? data.data : []; }
                },
                columns: [
                    {
                        data: 'created_at',
                        render: function(data) {
                            return data ? new Date(data).toLocaleString() : '';
                        }
                    },
                    { data: 'updated_by', defaultContent: '' },
                    { data: 'entity_type', defaultContent: '' },
                    { data: 'action', defaultContent: '' },
                    { data: 'reference_id', defaultContent: '' },
                    {
                        data: null,
                        orderable: false,
                        render: function(data, type, row) {
                            return self._summary(row);
                        }
                    },
                    {
                        data: null,
                        className: 'text-center',
                        orderable: false,
                        render: function(data) {
                            return '<button class="btn btn-sm btn-info btn-audit-details" data-log-id="' + data.log_id + '">' +
                                   '<i class="fa-regular fa-eye"></i></button>';
                        }
                    }
                ],
                order: [[0, 'desc']],
                pageLength: 25
            });

            // Entity filter (client-side custom search; guarded to this table)
            $.fn.dataTable.ext.search.push(function(settings, searchData, index, rowData) {
                if (settings.nTable.id !== 'audit-logs-table') return true;
                const val = $('#filter-audit-entity').val();
                if (val && rowData.entity_type !== val) return false;
                return true;
            });
            $('#filter-audit-entity').on('change', function() { self.auditTable.draw(); });
            $('#reset-audit-filters').on('click', function() {
                $('#filter-audit-entity').val('');
                self.auditTable.search('').draw();
            });

            // Show export button only when data exists
            self.auditTable.on('draw', function() {
                $('#export-audit-logs').toggle(self.auditTable.data().length > 0);
            });
            $('#export-audit-logs').on('click', function() { self.exportToExcel(); });

            // Details button
            $(document).on('click', '.btn-audit-details', function() {
                const row = self.auditTable.row($(this).closest('tr')).data();
                self.showDetails(row);
            });
        },

        _safeParse: function(str) {
            if (!str) return null;
            try { return typeof str === 'string' ? JSON.parse(str) : str; }
            catch (e) { return null; }
        },

        // Fields that change on every save (bookkeeping) — never shown in the diff table
        // active_project_count/other_active_project_count are computed read fields
        // (derived from the contractor_project mappings), not stored state — excluded
        // so audit diffs show only real edits.
        _diffIgnoreFields: ['created_at', 'create_at', 'updated_at', 'update_at', 'updated_by', 'log_id', 'active_project_count', 'other_active_project_count', 'project_positions', 'assignments'],

        _esc: function(v) {
            return String(v == null ? '' : v)
                .replace(/&/g, '&amp;').replace(/</g, '&lt;')
                .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
        },

        // Display value for a diff table cell: em-dash for empty, locale string for
        // ISO datetimes, compact JSON for objects/arrays, escaped text otherwise
        _formatVal: function(v) {
            if (v === null || v === undefined || v === '') return '<span class="text-muted">—</span>';
            if (typeof v === 'object') return this._esc(JSON.stringify(v));
            if (typeof v === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(v)) {
                const d = new Date(v);
                if (!isNaN(d)) return this._esc(d.toLocaleString());
            }
            return this._esc(v);
        },

        // Rows for the diff table. UPDATE: only keys where old !== new (JSON.stringify
        // compare, so type changes like 1 vs "1" count as changes). CREATE/DELETE:
        // all non-ignored keys of whichever side exists.
        _changes: function(from, to) {
            const rows = [];
            const base = from || to;
            if (!base || typeof base !== 'object') return rows;
            const keys = Object.keys(base);
            if (from && to) {
                Object.keys(to).forEach(function(k) { if (!keys.includes(k)) keys.push(k); });
            }
            const self = this;
            keys.forEach(function(k) {
                if (self._diffIgnoreFields.includes(k)) return;
                const oldVal = from ? from[k] : undefined;
                const newVal = to ? to[k] : undefined;
                if (from && to && JSON.stringify(oldVal) === JSON.stringify(newVal)) return;
                rows.push({ field: k, old: oldVal, new: newVal });
            });
            return rows;
        },

        // One-line human-readable descriptor per entity, read from data_to (or data_from
        // on a delete, where data_to is null). Falls back to '' for unknown entity types.
        _descriptor: function(row) {
            if (!row) return '';
            const src = this._safeParse(row.data_to) || this._safeParse(row.data_from);
            if (!src) return '';
            switch (row.entity_type) {
                case 'provider':      return [src.provider_code, src.provider_name].filter(Boolean).join(' — ');
                case 'project':       return [src.project_code, src.project_name].filter(Boolean).join(' — ');
                case 'contractor':    return [src.employee_id, src.name].filter(Boolean).join(' — ');
                case 'system_config': return src.key || '';
                case 'timelog':       return [src.attendance_id != null ? '#' + src.attendance_id : null, src.employee_id].filter(Boolean).join(' — ');
                default:              return '';
            }
        },

        _summary: function(row) {
            if (!row) return '';
            const to = this._safeParse(row.data_to);
            if (row.entity_type === 'bulk_enrollment' && to) {
                return '<span class="badge bg-success">' + (to.success_count || 0) + ' enrolled</span> ' +
                       (to.merged_count ? '<span class="badge bg-warning text-dark">' + to.merged_count + ' updated existing</span> ' : '') +
                       '<span class="badge bg-danger">' + (to.error_count || 0) + ' failed</span>' +
                       (to.file_name ? '<div class="text-muted small">' + to.file_name + '</div>' : '');
            }
            const desc = this._descriptor(row);
            return desc ? '<span class="small">' + desc + '</span>' : '<span class="text-muted small">—</span>';
        },

        showDetails: function(row) {
            if (!row) return;
            const self = this;
            const to = this._safeParse(row.data_to);
            const from = this._safeParse(row.data_from);

            const meta = '<dl class="row mb-0">' +
                '<dt class="col-sm-3">Date / Time</dt><dd class="col-sm-9">' + (row.created_at ? new Date(row.created_at).toLocaleString() : '') + '</dd>' +
                '<dt class="col-sm-3">Admin</dt><dd class="col-sm-9">' + (row.updated_by || '') + '</dd>' +
                '<dt class="col-sm-3">Entity</dt><dd class="col-sm-9">' + (row.entity_type || '') + '</dd>' +
                '<dt class="col-sm-3">Action</dt><dd class="col-sm-9">' + (row.action || '') + '</dd>' +
                '<dt class="col-sm-3">Reference</dt><dd class="col-sm-9">' + (row.reference_id || '') + '</dd>' +
                '</dl>';

            let body = '';
            if (row.entity_type === 'bulk_enrollment' && to) {
                body += '<div class="mb-2">' +
                    '<span class="badge bg-success me-1">' + (to.success_count || 0) + ' enrolled</span>' +
                    (to.merged_count ? '<span class="badge bg-warning text-dark me-1">' + to.merged_count + ' updated existing</span>' : '') +
                    '<span class="badge bg-danger me-1">' + (to.error_count || 0) + ' failed</span>' +
                    '<span class="badge bg-secondary">' + (to.total || 0) + ' total</span>' +
                    (to.provider_code ? '<div class="text-muted small mt-1">Provider: ' + to.provider_code + '</div>' : '') +
                    (to.project_code ? '<div class="text-muted small">Project: ' + to.project_code + '</div>' : '') +
                    '</div>';
                if (to.enrolled_employee_ids && to.enrolled_employee_ids.length) {
                    body += '<div class="mb-2"><strong>Enrolled IDs:</strong> ' + to.enrolled_employee_ids.join(', ') + '</div>';
                }
                if (to.merged_employee_ids && to.merged_employee_ids.length) {
                    body += '<div class="mb-2"><strong>Updated existing IDs:</strong> ' + to.merged_employee_ids.join(', ') + '</div>';
                }
                if (to.errors && to.errors.length) {
                    body += '<strong>Failed rows:</strong>' +
                        '<table class="table table-sm table-bordered"><thead><tr><th>Row</th><th>Name</th><th>Reason</th></tr></thead><tbody>' +
                        to.errors.map(function(e) {
                            return '<tr><td>' + e.row + '</td><td>' + (e.name || '') + '</td><td>' + e.message + '</td></tr>';
                        }).join('') +
                        '</tbody></table>';
                }
            } else {
                const changes = this._changes(from, to);
                let heading;
                if (from && to) heading = 'Changes (' + changes.length + ' field' + (changes.length === 1 ? '' : 's') + ' modified)';
                else if (to)    heading = 'Created Record (' + changes.length + ' fields)';
                else            heading = 'Deleted Record (' + changes.length + ' fields)';

                body += '<strong>' + heading + ':</strong>';
                if (changes.length) {
                    body += '<table class="table table-sm table-bordered"><thead><tr>' +
                            '<th style="width:26%">Field</th><th style="width:37%">Old (data_from)</th><th style="width:37%">New (data_to)</th>' +
                            '</tr></thead><tbody>' +
                            changes.map(function(r) {
                                return '<tr class="table-warning"><td>' + self._esc(r.field.replace(/_/g, ' ')) + '</td>' +
                                       '<td>' + self._formatVal(r.old) + '</td><td>' + self._formatVal(r.new) + '</td></tr>';
                            }).join('') +
                            '</tbody></table>';
                } else {
                    body += '<div class="text-muted">No field changes recorded.</div>';
                }
            }

            $('#audit-details-meta').html(meta);
            $('#audit-details-body').html(body);
            var modal = new bootstrap.Modal(document.getElementById('audit-log-details-modal'));
            modal.show();
        },

        exportToExcel: function() {
            const self = this;
            const tableData = this.auditTable.rows({ search: 'applied' }).data().toArray();
            if (tableData.length === 0) {
                AdminPage.common.showWarning('No data available to export', 'No Data');
                return;
            }

            const exportData = tableData.map(function(row) {
                const to = self._safeParse(row.data_to);
                let summary = '';
                if (row.entity_type === 'bulk_enrollment' && to) {
                    summary = (to.success_count || 0) + ' enrolled' +
                        (to.merged_count ? ' / ' + to.merged_count + ' updated' : '') +
                        ' / ' + (to.error_count || 0) + ' failed';
                } else {
                    summary = self._descriptor(row);
                }
                return {
                    'Date / Time': row.created_at ? new Date(row.created_at).toLocaleString() : '',
                    'Admin': row.updated_by || '',
                    'Entity': row.entity_type || '',
                    'Action': row.action || '',
                    'Reference': row.reference_id || '',
                    'Summary': summary
                };
            });

            const ws = XLSX.utils.json_to_sheet(exportData);
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, 'Audit Logs');
            const ts = new Date().toISOString().slice(0, 19).replace(/:/g, '-').replace('T', '_');
            XLSX.writeFile(wb, 'AuditLogs_' + ts + '.xlsx');
            AdminPage.common.showSuccess('Exported ' + tableData.length + ' audit log entries', 'Export Successful');
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
                        className: 'text-center',
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

            // Default the view to the last 7 days (today + 6 prior days, inclusive)
            const today = new Date();
            const sevenDaysAgo = new Date();
            sevenDaysAgo.setDate(today.getDate() - 6);
            $('#filter-from-date').val(self.toDateInputValue(sevenDaysAgo));
            $('#filter-to-date').val(self.toDateInputValue(today));

            const defaultFromDate = $('#filter-from-date').val();
            const defaultToDate = $('#filter-to-date').val();

            // Initialize DataTable
            self.timeLogTable = $('#timelogs-table').DataTable({
                ajax: {
                    url: self.buildFilterUrl(defaultFromDate, defaultToDate, ''),
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
                    { data: 'name' },
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
                        className: 'text-center',
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
                        className: 'text-center',
                        render: function(data) {
                            if (data === 'UNDERSTOOD') {
                                return '<span class="badge bg-success">Understood</span>';
                            } else if (data === 'NOT_UNDERSTOOD') {
                                return '<span class="badge bg-warning">Not Understood</span>';
                            }
                            return data || '-';
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
                        className: 'text-center',
                        render: function(data) {
                            return `
                                <button class="btn btn-sm btn-warning btn-edit" data-attendance-id="${data.attendance_id}" title="Edit">
                                    <i class="fa-regular fa-pen-to-square"></i>
                                </button>
                                <button class="btn btn-sm btn-danger btn-delete" data-attendance-id="${data.attendance_id}" title="Delete">
                                    <i class="fa-regular fa-trash-can"></i>
                                </button>
                            `;
                        }
                    }
                ],
                order: [[0, 'desc']], // Sort by ID descending
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
                const rowData = self.timeLogTable.row($(this).closest('tr')).data();

                $.ajax({
                    url: '/Admin/GetTimeLogById',
                    method: 'GET',
                    data: { attendance_id: attendanceId },
                    success: function(response) {
                        if (response.success && response.data) {
                            const timelog = response.data;

                            $('#edit_attendance_id').val(timelog.attendance_id);
                            $('#edit_employee_id').val(timelog.employee_id || '');
                            $('#edit_employee_name').val(rowData ? rowData.name : '');
                            $('#edit_time_in').val(self.formatDateTimeForInput(timelog.time_in));
                            $('#edit_time_out').val(timelog.time_out ? self.formatDateTimeForInput(timelog.time_out) : '');
                            $('#edit_health_status').val(timelog.health_status);
                            $('#edit_waiver_consent').val(timelog.waiver_consent);

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
                    health_status: $('#edit_health_status').val(),
                    waiver_consent: $('#edit_waiver_consent').val()
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
            const fromDate = $('#filter-from-date').val();
            const toDate = $('#filter-to-date').val();
            const healthStatus = $('#filter-health-status').val();

            // Reload DataTable with filters
            this.timeLogTable.ajax.url(this.buildFilterUrl(fromDate, toDate, healthStatus)).load();
        },

        clearFilters: function() {
            $('#filter-from-date').val('');
            $('#filter-to-date').val('');
            $('#filter-health-status').val('');

            // Reload DataTable without filters
            this.timeLogTable.ajax.url('/Admin/GetAllTimeLogs').load();
        },

        buildFilterUrl: function(fromDate, toDate, healthStatus) {
            let url = '/Admin/GetAllTimeLogs?';
            const params = [];

            if (fromDate) params.push('from_date=' + encodeURIComponent(fromDate));
            if (toDate) params.push('to_date=' + encodeURIComponent(toDate));
            if (healthStatus) params.push('health_status=' + encodeURIComponent(healthStatus));

            return url + params.join('&');
        },

        toDateInputValue: function(date) {
            // Format a Date as YYYY-MM-DD using LOCAL date parts
            // (avoids the UTC off-by-one that toISOString() would cause for early-hours local times)
            const y = date.getFullYear();
            const m = String(date.getMonth() + 1).padStart(2, '0');
            const d = String(date.getDate()).padStart(2, '0');
            return y + '-' + m + '-' + d;
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
                'Employee Name': row.name,
                'Time In': row.time_in ? AdminPage.common.formatDateTime(row.time_in) : '-',
                'Time Out': row.time_out ? AdminPage.common.formatDateTime(row.time_out) : 'Active',
                'Health Status': row.health_status,
                'Waiver Consent': row.waiver_consent === 'UNDERSTOOD' ? 'Understood' : row.waiver_consent === 'NOT_UNDERSTOOD' ? 'Not Understood' : row.waiver_consent || '-',
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

    // Index.cshtml - Admin dashboard (stat cards + Recent TimeLogs + ApexCharts)
    index: {
        recentTable: null,        // Recent TimeLogs DataTable
        charts: {},               // ApexCharts instances keyed 'attendance' | 'health' | 'consent'
        stats: null,              // cached /Admin/GetDashboardStats payload
        providers: [],            // active providers (from /Admin/GetAllProviders)
        projects: [],             // active projects (from /Admin/GetAllProjects, filtered active===1)
        loadingStats: false,      // guard against double-Apply
        currentAxis: [],          // ['YYYY-MM-DD', ...] currently displayed
        currentLabels: [],        // display labels for the axis (e.g. 'Sep 5')

        cards: {
            attendance: {
                dimension: '#chart-attendance-dimension',
                entity: '#chart-attendance-entity',
                mount: 'chart-attendance',
                colors: ['#0d6efd'],
                stacked: false
            },
            health: {
                dimension: '#chart-health-dimension',
                entity: '#chart-health-entity',
                mount: 'chart-health',
                colors: ['#198754', '#dc3545'],   // FIT green / UNFIT red (badge colors)
                stacked: true
            },
            consent: {
                dimension: '#chart-consent-dimension',
                entity: '#chart-consent-entity',
                mount: 'chart-consent',
                colors: ['#198754', '#ffc107'],   // UNDERSTOOD green / NOT_UNDERSTOOD amber (badge colors)
                stacked: true
            }
        },

        init: function() {
            const self = AdminPage.index;

            // Default the shared date range to the last 7 days (today-6 .. today)
            const today = new Date();
            const sevenDaysAgo = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 6);
            $('#chart-from-date').val(AdminPage.timeLogs.toDateInputValue(sevenDaysAgo));
            $('#chart-to-date').val(AdminPage.timeLogs.toDateInputValue(today));

            // Recent TimeLogs DataTable (moved from the inline view script)
            self.recentTable = $('#recentTimeLogsTable').DataTable({
                ajax: {
                    url: '/Admin/GetRecentTimeLogs',
                    dataSrc: 'data',
                    type: 'GET'
                },
                columns: [
                    { data: 'employee_id' },
                    { data: 'name' },
                    {
                        data: 'provider_code',
                        render: function(data, type, row) {
                            return row.provider_name && row.provider_code
                                ? `${row.provider_name} (${row.provider_code})`
                                : row.provider_code || 'N/A';
                        }
                    },
                    {
                        data: 'project_code',
                        render: function(data, type, row) {
                            return row.project_name && row.project_code
                                ? `${row.project_name} (${row.project_code})`
                                : row.project_code || 'N/A';
                        }
                    },
                    {
                        data: 'time_in',
                        render: function(data) {
                            return data ? new Date(data).toLocaleString() : 'N/A';
                        }
                    },
                    {
                        data: 'time_out',
                        render: function(data) {
                            return data ? new Date(data).toLocaleString() : '<span class="badge bg-success">Active</span>';
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
                                return '<span class="badge bg-success">UNDERSTOOD</span>';
                            } else if (data === 'NOT_UNDERSTOOD') {
                                return '<span class="badge bg-warning text-dark">NOT UNDERSTOOD</span>';
                            }
                            return data || 'N/A';
                        }
                    }
                ],
                order: [[4, 'desc']],
                pageLength: 10,
                language: {
                    emptyTable: 'No recent activity found'
                }
            });

            // Wire chart events
            $('#apply-chart-range').on('click', function() {
                self.loadStats();
            });
            Object.keys(self.cards).forEach(function(key) {
                $(self.cards[key].dimension).on('change', function() {
                    self.onDimensionChange(key);
                });
                $(self.cards[key].entity).on('change', function() {
                    self.refreshCard(key);   // re-render from cached payload, no refetch
                });
            });

            // Create the three charts once, then load real data
            Object.keys(self.cards).forEach(function(key) {
                const cfg = self.cards[key];
                self.renderChart(key, cfg.mount, self.chartBaseOptions(cfg.colors, cfg.stacked));
            });

            self.loadDropdownSources();
            self.loadStats();

            // Initialize sidebar
            if (AdminPage.sidebar) {
                AdminPage.sidebar.init();
            }
        },

        loadDropdownSources: function() {
            const self = AdminPage.index;

            // GetAllProviders is already active-filtered server-side
            $.ajax({
                url: '/Admin/GetAllProviders',
                method: 'GET',
                success: function(response) {
                    if (response.success) {
                        self.providers = response.data || [];
                        self.refreshEntitySelects();
                    }
                }
            });

            // GetAllProjects only filters is_deleted - inactive projects must be filtered here
            $.ajax({
                url: '/Admin/GetAllProjects',
                method: 'GET',
                success: function(response) {
                    if (response.success) {
                        self.projects = (response.data || []).filter(function(p) { return p.active === 1; });
                        self.refreshEntitySelects();
                    }
                }
            });
        },

        refreshEntitySelects: function() {
            // Re-populate any open entity select after providers/projects finish loading
            const self = AdminPage.index;
            Object.keys(self.cards).forEach(function(key) {
                if ($(self.cards[key].dimension).val() !== 'overall') {
                    self.onDimensionChange(key);
                }
            });
        },

        buildStatsUrl: function(fromDate, toDate) {
            let url = '/Admin/GetDashboardStats?';
            const params = [];

            if (fromDate) params.push('from_date=' + encodeURIComponent(fromDate));
            if (toDate) params.push('to_date=' + encodeURIComponent(toDate));

            return url + params.join('&');
        },

        loadStats: function() {
            const self = AdminPage.index;
            if (self.loadingStats) return;

            const fromDate = $('#chart-from-date').val();
            const toDate = $('#chart-to-date').val();
            if (!fromDate || !toDate) {
                AdminPage.common.showError('Please select both From and To dates.');
                return;
            }

            const from = self.parseDay(fromDate);
            const to = self.parseDay(toDate);
            if (from > to) {
                AdminPage.common.showError('From date must be on or before To date.');
                return;
            }
            if ((to - from) / 86400000 > 366) {
                AdminPage.common.showError('Date range cannot exceed one year.');
                return;
            }

            self.loadingStats = true;
            $.ajax({
                url: self.buildStatsUrl(fromDate, toDate),
                method: 'GET',
                success: function(response) {
                    if (response.success && response.data) {
                        self.stats = response.data;
                        self.refreshAllCharts();
                    } else {
                        AdminPage.common.showError(response.message || 'Failed to load dashboard stats');
                    }
                },
                error: function() {
                    AdminPage.common.showError('Failed to load dashboard stats');
                },
                complete: function() {
                    self.loadingStats = false;
                }
            });
        },

        parseDay: function(dayString) {
            // Parse 'YYYY-MM-DD' with LOCAL date parts
            // (never feed the plain string to new Date() - that parses as UTC)
            const parts = dayString.split('-').map(Number);
            return new Date(parts[0], parts[1] - 1, parts[2]);
        },

        buildDateAxis: function(fromDate, toDate) {
            // Inclusive ['YYYY-MM-DD', ...] axis; advance with local date arithmetic
            const axis = [];
            for (let d = this.parseDay(fromDate); d <= this.parseDay(toDate); d = new Date(d.getFullYear(), d.getMonth(), d.getDate() + 1)) {
                axis.push(AdminPage.timeLogs.toDateInputValue(d));
            }
            return axis;
        },

        selectRows: function(dimension, entityCode) {
            if (!this.stats) return [];
            if (dimension === 'provider') {
                return (this.stats.by_provider || []).filter(function(r) { return r.provider_code === entityCode; });
            }
            if (dimension === 'project') {
                return (this.stats.by_project || []).filter(function(r) { return r.project_code === entityCode; });
            }
            return this.stats.overall || [];
        },

        dayMap: function(rows) {
            // { 'YYYY-MM-DD': row } for zero-fill lookups against the axis
            const map = {};
            (rows || []).forEach(function(r) {
                map[r.stat_date ? r.stat_date.slice(0, 10) : ''] = r;
            });
            return map;
        },

        getSeriesForCard: function(key) {
            const self = AdminPage.index;
            const cfg = self.cards[key];
            const rows = self.selectRows($(cfg.dimension).val(), $(cfg.entity).val());
            const map = self.dayMap(rows);
            const count = function(day, field) { return map[day] ? map[day][field] : 0; };

            if (key === 'attendance') {
                return [{ name: 'Attendance', data: self.currentAxis.map(function(d) { return count(d, 'total_count'); }) }];
            }
            if (key === 'health') {
                return [
                    { name: 'FIT', data: self.currentAxis.map(function(d) { return count(d, 'fit_count'); }) },
                    { name: 'UNFIT', data: self.currentAxis.map(function(d) { return count(d, 'unfit_count'); }) }
                ];
            }
            return [
                { name: 'UNDERSTOOD', data: self.currentAxis.map(function(d) { return count(d, 'understood_count'); }) },
                { name: 'NOT_UNDERSTOOD', data: self.currentAxis.map(function(d) { return count(d, 'not_understood_count'); }) }
            ];
        },

        chartBaseOptions: function(colors, stacked) {
            return {
                // ApexCharts v5 requires `series` at construction; empty data shows
                // the noData overlay until the first loadStats() replaces it
                series: [{ name: '', data: [] }],
                chart: {
                    type: 'bar',
                    stacked: stacked,
                    height: 300,
                    fontFamily: 'inherit',
                    toolbar: { show: false },
                    animations: { enabled: false }
                },
                plotOptions: {
                    bar: { columnWidth: '55%' }
                },
                dataLabels: { enabled: false },
                colors: colors,
                xaxis: {
                    categories: [],
                    labels: { rotate: -45, style: { fontSize: '11px' } },
                    tooltip: { enabled: false }
                },
                yaxis: {
                    min: 0,
                    forceNiceScale: true,
                    labels: { formatter: function(v) { return Math.round(v); } }
                },
                legend: { position: 'bottom', show: stacked },
                noData: { text: 'No data for the selected period', align: 'center' },
                // ApexCharts v5 defaults tooltip.intersect to true; shared requires intersect:false
                tooltip: { shared: true, intersect: false }
            };
        },

        renderChart: function(key, mountId, options) {
            if (this.charts[key]) return;   // create-once
            const el = document.getElementById(mountId);
            if (!el || typeof ApexCharts === 'undefined') return;
            this.charts[key] = new ApexCharts(el, options);
            this.charts[key].render();
        },

        refreshAllCharts: function() {
            const self = AdminPage.index;
            const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

            self.currentAxis = self.buildDateAxis($('#chart-from-date').val(), $('#chart-to-date').val());
            self.currentLabels = self.currentAxis.map(function(d) {
                const p = d.split('-');
                return MONTHS[Number(p[1]) - 1] + ' ' + Number(p[2]);
            });

            Object.keys(self.cards).forEach(function(key) {
                self.refreshCard(key);
            });
        },

        refreshCard: function(key) {
            const self = AdminPage.index;
            const chart = self.charts[key];
            if (!chart) return;

            // updateOptions (not updateSeries) so the category axis moves on range changes
            chart.updateOptions({
                series: self.getSeriesForCard(key),
                xaxis: { categories: self.currentLabels }
            }, false, true);
        },

        onDimensionChange: function(key) {
            const self = AdminPage.index;
            const cfg = self.cards[key];
            const dimension = $(cfg.dimension).val();
            const $entity = $(cfg.entity);

            if (dimension === 'overall') {
                $entity.empty().addClass('d-none').prop('disabled', true);
            } else {
                const source = dimension === 'provider' ? self.providers : self.projects;
                $entity.empty();
                source.forEach(function(e) {
                    const code = e.provider_code || e.project_code;
                    const name = e.provider_name || e.project_name || code;
                    $entity.append($('<option></option>').attr('value', code).text(name + ' (' + code + ')'));
                });
                $entity.removeClass('d-none').prop('disabled', false);
            }
            self.refreshCard(key);
        }
    }
};

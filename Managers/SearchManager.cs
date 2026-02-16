using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GestioneCespiti.Models;
using GestioneCespiti.Services;

namespace GestioneCespiti.Managers
{
    public class SearchManager
    {
        private readonly DataPersistenceService _persistenceService;
        private List<SearchResult> _searchResults = new List<SearchResult>();
        private int _currentSearchIndex = -1;

        public event EventHandler<SearchCompletedEventArgs>? SearchCompleted;
        public event EventHandler<SearchNavigateEventArgs>? NavigateRequested;

        public SearchManager(DataPersistenceService persistenceService)
        {
            _persistenceService = persistenceService;
        }

        public void PerformSearch(string searchText, bool includeArchived, bool matchCase, bool showUserMessages = true)
        {
            _searchResults.Clear();
            _currentSearchIndex = -1;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                SearchCompleted?.Invoke(this, new SearchCompletedEventArgs(0, false));
                if (showUserMessages)
                {
                    MessageBox.Show("Inserisci un testo da cercare.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            var allSheets = _persistenceService.LoadAllSheets(includeArchived);
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (var sheet in allSheets)
            {
                for (int rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
                {
                    var asset = sheet.Rows[rowIndex];
                    foreach (var column in sheet.Columns)
                    {
                        string value = asset[column];
                        if (value.IndexOf(searchText, comparison) >= 0)
                        {
                            _searchResults.Add(new SearchResult
                            {
                                Sheet = sheet,
                                RowIndex = rowIndex,
                                ColumnName = column,
                                Value = value
                            });
                        }
                    }
                }
            }

            if (_searchResults.Count == 0)
            {
                if (showUserMessages)
                {
                    MessageBox.Show($"Nessun risultato trovato per '{searchText}'.", "Ricerca", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                SearchCompleted?.Invoke(this, new SearchCompletedEventArgs(0, false));
                return;
            }

            _currentSearchIndex = 0;
            NavigateRequested?.Invoke(this, new SearchNavigateEventArgs(_searchResults[0]));
            SearchCompleted?.Invoke(this, new SearchCompletedEventArgs(_searchResults.Count, true));
        }

        public bool NavigateNext()
        {
            if (_searchResults.Count == 0)
                return false;

            if (_currentSearchIndex + 1 >= _searchResults.Count)
            {
                MessageBox.Show("Non ci sono altre ricorrenze.", "Ricerca", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            _currentSearchIndex++;
            NavigateRequested?.Invoke(this, new SearchNavigateEventArgs(_searchResults[_currentSearchIndex]));
            return true;
        }

        public int CurrentIndex => _currentSearchIndex;
        public int TotalResults => _searchResults.Count;
        public bool HasResults => _searchResults.Count > 0;
    }

    public class SearchCompletedEventArgs : EventArgs
    {
        public int ResultCount { get; }
        public bool HasResults { get; }

        public SearchCompletedEventArgs(int resultCount, bool hasResults)
        {
            ResultCount = resultCount;
            HasResults = hasResults;
        }
    }

    public class SearchNavigateEventArgs : EventArgs
    {
        public SearchResult Result { get; }

        public SearchNavigateEventArgs(SearchResult result)
        {
            Result = result;
        }
    }
}

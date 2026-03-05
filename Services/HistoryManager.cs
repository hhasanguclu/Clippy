using System;
using System.Collections.Generic;
using Clippy.Models;

namespace Clippy.Services
{
    public class HistoryManager
    {
        private readonly DatabaseService _db;
        private int _maxEntries = 200;

        public HistoryManager(DatabaseService db)
        {
            _db = db;
        }

        public int MaxEntries
        {
            get => _maxEntries;
            set
            {
                _maxEntries = Math.Max(10, value);
                _db.EnforceMaxEntries(_maxEntries);
            }
        }

        public void Add(ClipboardEntry entry)
        {
            // Check for duplicate
            var existing = _db.FindByHash(entry.ContentHash);
            if (existing != null)
            {
                // Move to top by updating timestamp
                _db.UpdateTimestamp(existing.Id);
            }
            else
            {
                entry.Id = _db.Insert(entry);
                _db.EnforceMaxEntries(_maxEntries);
            }
        }

        public List<ClipboardEntry> Search(string query)
        {
            return _db.Search(query, _maxEntries);
        }

        public void TogglePin(long id)
        {
            _db.TogglePin(id);
        }

        public void Remove(long id)
        {
            _db.Delete(id);
        }

        public void Clear(bool keepPinned = true)
        {
            _db.Clear(keepPinned);
        }

        public ClipboardEntry? GetById(long id)
        {
            return _db.GetById(id);
        }

        public int Count => _db.GetCount();
    }
}

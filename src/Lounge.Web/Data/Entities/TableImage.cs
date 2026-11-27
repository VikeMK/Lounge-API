using System;
using System.ComponentModel.DataAnnotations;

namespace Lounge.Web.Data.Entities
{
    /// <summary>
    /// Records that a table's image exists in the image bucket (and when it was last updated).
    /// Kept separate from <see cref="Table"/> on purpose: Tables is change-tracked and every change there
    /// makes DbCache recompute all stats, so image bookkeeping must not write to it.
    /// This table is NOT change-tracked and not part of DbCache.
    /// </summary>
    public class TableImage
    {
        [Key]
        public int TableId { get; set; }

        /// <summary>
        /// When the image was last uploaded, or first found in the bucket (UTC). Informational only,
        /// e.g. to see when a table's image was last regenerated; the app only cares that the row exists.
        /// </summary>
        public DateTime UpdatedOn { get; set; }
    }
}

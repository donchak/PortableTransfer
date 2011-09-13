using System;
using System.Collections.Generic;
using System.Text;

namespace PortableTransfer {
    public static class TransferConst {
        public const string PortableTransferJournalMutex = "Global\\PortableTransferJournal_Mutex:{295aec71-ff22-47de-9a92-e78f1699cdb4}";
        public const string PortableTransferComputerGuidMutex = "Global\\PortableTransferComputerGuid_Mutex:{295aec71-ff22-47de-9a92-e78f1699cdb4}";
        public const string PortableTransferComputerGuidHeader = "HAGBIS Computer Guid v1.0";
        public const string BackupStorageHeader = "HAGBIS Backup Storage v1.0";
        public const string BackupListHeader = "HAGBIS Backup List v1.0";
        public const string PortableTransferJournalNextItemSign = "###NXT#ITEM###";
    }
}

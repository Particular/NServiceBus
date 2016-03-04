namespace NServiceBus
{
    using System;
    using System.ComponentModel;
    using System.Messaging;
    using System.Runtime.InteropServices;
    using System.Security.Principal;

    /// <summary>
    /// Reads the Access Control Entries (ACE) from an MSMQ queue.
    /// </summary>
    /// <remarks>
    /// There is no managed API for reading the queue permissions, this has to be done via P/Invoke. by calling
    /// <c>MQGetQueueSecurity</c> API.
    /// See http://stackoverflow.com/questions/10177255/how-to-get-the-current-permissions-for-an-msmq-private-queue
    /// </remarks>
    static class MessageQueueExtensions
    {
        [DllImport("mqrt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int MQGetQueueSecurity(string formatName, int SecurityInformation, IntPtr SecurityDescriptor, int length, out int lengthNeeded);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetSecurityDescriptorDacl(IntPtr pSD, out bool daclPresent, out IntPtr pDacl, out bool daclDefaulted);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool GetAclInformation(IntPtr pAcl, ref ACL_SIZE_INFORMATION pAclInformation, uint nAclInformationLength, ACL_INFORMATION_CLASS dwAclInformationClass);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetAce(IntPtr aclPtr, int aceIndex, out IntPtr acePtr);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetLengthSid(IntPtr pSID);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool ConvertSidToStringSid([MarshalAs(UnmanagedType.LPArray)] byte[] pSID, out IntPtr ptrSid);

        const string PREFIX_FORMAT_NAME = "FORMATNAME:";
        const int DACL_SECURITY_INFORMATION = 4;
        const int MQ_ERROR_SECURITY_DESCRIPTOR_TOO_SMALL = unchecked((int) 0xc00e0023);
        const int MQ_OK = 0;
        static bool administerGranted;

        //Security constants

        // the following constants taken from MessageQueue.cs (see http://referencesource.microsoft.com/#System.Messaging/System/Messaging/MessageQueue.cs)
        // ReSharper disable MemberCanBePrivate.Local
        [StructLayout(LayoutKind.Sequential)]
        struct ACE_HEADER
        {
            public byte AceType;
            public byte AceFlags;
            public short AceSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ACCESS_ALLOWED_ACE
        {
            public ACE_HEADER Header;
            public uint Mask;
            public int SidStart;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ACL_SIZE_INFORMATION
        {
            public uint AceCount;
            public uint AclBytesInUse;
            public uint AclBytesFree;
        }

        enum ACL_INFORMATION_CLASS
        {
            // ReSharper disable once UnusedMember.Local
            AclRevisionInformation = 1,
            AclSizeInformation
        }

        public static bool TryGetPermissions(this MessageQueue queue, string user, out MessageQueueAccessRights? rights)
        {
            if (!administerGranted)
            {
                var permission = new MessageQueuePermission(MessageQueuePermissionAccess.Administer, PREFIX_FORMAT_NAME + queue.FormatName);
                permission.Demand();

                administerGranted = true;
            }

            var sid = GetSidForUser(user);

            try
            {
                rights = GetPermissions(queue.FormatName, sid);
                return true;
            }
            catch
            {
                rights = null;
                return false;
            }
        }

        static MessageQueueAccessRights GetPermissions(string formatName, string sid)
        {
            var SecurityDescriptor = new byte[100];

            var sdHandle = GCHandle.Alloc(SecurityDescriptor, GCHandleType.Pinned);
            try
            {
                int lengthNeeded;
                var mqResult = MQGetQueueSecurity(formatName,
                    DACL_SECURITY_INFORMATION,
                    sdHandle.AddrOfPinnedObject(),
                    SecurityDescriptor.Length,
                    out lengthNeeded);

                if (mqResult == MQ_ERROR_SECURITY_DESCRIPTOR_TOO_SMALL)
                {
                    sdHandle.Free();
                    SecurityDescriptor = new byte[lengthNeeded];
                    sdHandle = GCHandle.Alloc(SecurityDescriptor, GCHandleType.Pinned);
                    mqResult = MQGetQueueSecurity(formatName,
                        DACL_SECURITY_INFORMATION,
                        sdHandle.AddrOfPinnedObject(),
                        SecurityDescriptor.Length,
                        out lengthNeeded);
                }

                if (mqResult != MQ_OK)
                {
                    throw new Exception($"Unable to read the security descriptor of queue [{formatName}]");
                }

                bool daclPresent, daclDefaulted;
                IntPtr pDacl;
                var success = GetSecurityDescriptorDacl(sdHandle.AddrOfPinnedObject(),
                    out daclPresent,
                    out pDacl,
                    out daclDefaulted);

                if (!success)
                {
                    throw new Win32Exception();
                }

                var allowedAce = GetAce(pDacl, sid);

                return (MessageQueueAccessRights) allowedAce.Mask;
            }
            finally
            {
                if (sdHandle.IsAllocated)
                {
                    sdHandle.Free();
                }
            }
        }

        static string GetSidForUser(string username)
        {
            var account = new NTAccount(username);
            var sid = (SecurityIdentifier) account.Translate(typeof(SecurityIdentifier));

            return sid.ToString();
        }

        static ACCESS_ALLOWED_ACE GetAce(IntPtr pDacl, string sid)
        {
            var AclSize = new ACL_SIZE_INFORMATION();
            GetAclInformation(pDacl, ref AclSize, (uint) Marshal.SizeOf(typeof(ACL_SIZE_INFORMATION)), ACL_INFORMATION_CLASS.AclSizeInformation);

            for (var i = 0; i < AclSize.AceCount; i++)
            {
                IntPtr pAce;
                GetAce(pDacl, i, out pAce);
                var ace = (ACCESS_ALLOWED_ACE) Marshal.PtrToStructure(pAce, typeof(ACCESS_ALLOWED_ACE));

                var iter = (IntPtr) ((long) pAce + (long) Marshal.OffsetOf(typeof(ACCESS_ALLOWED_ACE), "SidStart"));
                var size = GetLengthSid(iter);
                var bSID = new byte[size];
                Marshal.Copy(iter, bSID, 0, size);
                IntPtr ptrSid;
                ConvertSidToStringSid(bSID, out ptrSid);

                var strSID = Marshal.PtrToStringAuto(ptrSid);

                if (strSID == sid)
                {
                    return ace;
                }
            }

            throw new Exception($"No ACE for SID {sid} found in security descriptor");
        }
    }
}
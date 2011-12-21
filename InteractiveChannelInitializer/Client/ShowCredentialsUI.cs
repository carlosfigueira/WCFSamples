using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Client
{
    class ShowCredentialsUI : IInteractiveChannelInitializer
    {
        ClientForm clientForm;
        public ShowCredentialsUI(ClientForm clientForm)
        {
            this.clientForm = clientForm;
        }

        public IAsyncResult BeginDisplayInitializationUI(IClientChannel channel, AsyncCallback callback, object state)
        {
            return new ClientPasswordAsyncResult(this.clientForm, channel, callback, state);
        }

        public void EndDisplayInitializationUI(IAsyncResult result)
        {
            string userName, password;
            IClientChannel clientChannel;
            ClientPasswordAsyncResult.End(result, out userName, out password, out clientChannel);
            ChannelParameterCollection coll = clientChannel.GetProperty<ChannelParameterCollection>();
            coll.Add(new NetworkCredential(userName, password));
        }

        class ClientPasswordAsyncResult : AsyncResult
        {
            private delegate void ShowPasswordDialogDelegate();
            ClientForm clientForm;
            ClientPasswordDialog passwordForm;
            IClientChannel clientChannel;

            public ClientPasswordAsyncResult(ClientForm clientForm, IClientChannel clientChannel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.clientForm = clientForm;
                this.clientChannel = clientChannel;
                this.passwordForm = new ClientPasswordDialog();
                this.clientForm.BeginInvoke(new ShowPasswordDialogDelegate(delegate
                {
                    this.passwordForm.ShowDialog();
                    this.Complete(false);
                }));
            }

            public static void End(IAsyncResult asyncResult, out string userName, out string password, out IClientChannel channel)
            {
                ClientPasswordAsyncResult thisPtr = AsyncResult.End<ClientPasswordAsyncResult>(asyncResult);
                userName = thisPtr.passwordForm.UserName;
                password = thisPtr.passwordForm.Password;
                channel = thisPtr.clientChannel;
            }
        }
    }
}

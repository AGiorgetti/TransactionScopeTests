using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Transactions;

namespace TransactionScopeTests
{
    [TestFixture]
    public class TransactionScopeTests
    {
        /// <summary>
        /// If you use the default option TransactionScopeOption.Required then the nested scope 
        /// will enlist in the same transaction as the outer scope and as such when the outer 
        /// scope rolls back the inner scope will also be rolled back even if it has called
        /// Complete.
        /// </summary>
        [Test]
        public void TransactionScopeAffectsCurrentTransaction_Required()
        {
            Assert.IsTrue(Transaction.Current == null);

            using (var tx = new TransactionScope(TransactionScopeOption.Required))
            {
                Assert.IsTrue(Transaction.Current != null);
                Assert.IsTrue(Transaction.Current.IsolationLevel == IsolationLevel.Serializable);

                SomeMethodInTheCallStack_AssertActiveTransaction();

                tx.Complete();
            }

            Assert.IsTrue(Transaction.Current == null);
        }

        /// <summary>
        /// If, however, you use TransactionScopeOption.RequiresNew then the nested scope 
        /// will begin its own transaction and complete it separately from the outer scope,
        /// so it will not roll back even if the outer scope rolls back.
        /// </summary>
        [Test]
        public void TransactionScopeAffectsCurrentTransaction_RequiresNew()
        {
            Assert.IsTrue(Transaction.Current == null);

            using (var tx = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                Assert.IsTrue(Transaction.Current != null);
                Assert.IsTrue(Transaction.Current.IsolationLevel == IsolationLevel.Serializable);

                SomeMethodInTheCallStack_AssertActiveTransaction();

                tx.Complete();
            }

            Assert.IsTrue(Transaction.Current == null);
        }


        /// <summary>
        /// If you use TransactionScopeOption.Suppress then the nested scope will not take 
        /// part in the outer transaction and will complete non-transactionally, thus does 
        /// not form part of the work that would be rolled back if the outer transaction 
        /// rolls back.
        /// </summary>
        [Test]
        public void TransactionScopeAffectsCurrentTransaction_Suppress()
        {
            Assert.IsTrue(Transaction.Current == null);

            using (var outerTx = new TransactionScope(TransactionScopeOption.Required))
            {
                Assert.IsTrue(Transaction.Current != null);

                using (var tx = new TransactionScope(TransactionScopeOption.Suppress))
                {
                    Assert.IsTrue(Transaction.Current == null);

                    SomeMethodInTheCallStack_AssertNoActiveTRansaction();

                    tx.Complete();
                }

                Assert.IsTrue(Transaction.Current != null);
            }

            Assert.IsTrue(Transaction.Current == null);
        }

        /// <summary>
        /// this test fails! the default settings for TransactionScope does NOT
        /// play well with async/await: must specify: TransactionScopeAsyncFlowOption
        /// </summary>
        [Test]
        [Explicit]
        public void TransactionScope_AsyncAwait_Throws_InvalidOperationException()
        {
            TransactionScopeAsyncAwaitTest().Wait();
        }

        private static async Task TransactionScopeAsyncAwaitTest()
        {
            Assert.IsTrue(Transaction.Current == null);

            using (var tx = new TransactionScope())
            {
                Assert.IsTrue(Transaction.Current != null);

                await SomeMethodInTheCallStackAsync()
                    .ConfigureAwait(false);

                tx.Complete();
            }

            Assert.IsTrue(Transaction.Current == null);
        }


#if NET451 || NETSTANDARD2_0
        /// <summary>
        /// The solution is valid net451 and up
        /// </summary>
        /// <returns></returns>
        [Test]
        public void TransactionScope_Fixed_For_AsyncAwait()
        {
            TransactionScopeAsyncAwaitTestFixed().GetAwaiter().GetResult();
        }

        private static async Task TransactionScopeAsyncAwaitTestFixed()
        {
            Assert.IsTrue(Transaction.Current == null);

            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                Assert.IsTrue(Transaction.Current != null);

                await SomeMethodInTheCallStackAsync()
                    .ConfigureAwait(false);

                tx.Complete();
            }

            Assert.IsTrue(Transaction.Current == null);
        }
#endif

        private static async Task SomeMethodInTheCallStackAsync()
        {
            await Task.Delay(500).ConfigureAwait(false);
        }

        private static void SomeMethodInTheCallStack_AssertActiveTransaction()
        {
            Assert.IsTrue(Transaction.Current != null);
        }

        private static void SomeMethodInTheCallStack_AssertNoActiveTRansaction()
        {
            Assert.IsTrue(Transaction.Current == null);
        }
    }
}

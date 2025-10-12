// The MIT License(MIT)
// 
// Copyright (c) 2017 Hangfire OÜ
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
#if !NETSTANDARD1_0
using System.Runtime.Serialization;
#endif

namespace Cronos
{
    /// <summary>
    /// Represents an exception thrown when the cron expression requires jitter, but a seed wasn't provided
    /// </summary>
#if !NETSTANDARD1_0
    [Serializable]
#endif
    public class MissingSeedException : Exception
    {
        internal const string BaseMessage = "Using H in the format requires providing a jitter seed.";

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingSeedException"/> class.
        /// </summary>
        public MissingSeedException() : this(BaseMessage)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingSeedException"/> class with
        /// a specified error message.
        /// </summary>
        public MissingSeedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingSeedException"/> class with
        /// a specified error message and a reference to the inner exception that is the
        /// cause of this exception.
        /// </summary>
        public MissingSeedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !NETSTANDARD1_0
        /// <inheritdoc />
        protected MissingSeedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif
    }
}
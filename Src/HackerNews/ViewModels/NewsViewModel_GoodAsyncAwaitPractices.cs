﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;

using HackerNews.Shared;

namespace HackerNews
{
    public class NewsViewModel_GoodAsyncAwaitPractices : BaseViewModel
    {
        #region Constant Fields
        readonly WeakEventManager<string> _errorOcurredEventManager = new WeakEventManager<string>();
        #endregion

        #region Fields
        bool _isListRefreshing;
        IAsyncCommand _refreshCommand;
        List<StoryModel> _topStoryList;
        #endregion

        #region Constructors
        public NewsViewModel_GoodAsyncAwaitPractices()
        {
            ExecuteRefreshCommand().SafeFireAndForget();
        }
        #endregion

        #region Events
        public event EventHandler<string> ErrorOcurred
        {
            add => _errorOcurredEventManager.AddEventHandler(value);
            remove => _errorOcurredEventManager.RemoveEventHandler(value);
        }
        #endregion

        #region Properties
        public IAsyncCommand RefreshCommand => _refreshCommand ??
            (_refreshCommand = new AsyncCommand(ExecuteRefreshCommand, continueOnCapturedContext: false));

        public List<StoryModel> TopStoryList
        {
            get => _topStoryList;
            set => SetProperty(ref _topStoryList, value);
        }

        public bool IsListRefreshing
        {
            get => _isListRefreshing;
            set => SetProperty(ref _isListRefreshing, value);
        }
        #endregion

        #region Methods
        async Task ExecuteRefreshCommand()
        {
            IsListRefreshing = true;

            try
            {
                TopStoryList = await GetTopStories(StoriesConstants.NumberOfStories).ConfigureAwait(false);
            }
            finally
            {
                IsListRefreshing = false;
            }
        }

        async Task<List<StoryModel>> GetTopStories(int numberOfStories)
        {
            var topStoryIds = await GetTopStoryIDs().ConfigureAwait(false);

            var getTopStoryTaskList = new List<Task<StoryModel>>();
            for (int i = 0; i < Math.Min(topStoryIds.Count, numberOfStories); i++)
            {
                getTopStoryTaskList.Add(GetStory(topStoryIds[i]));
            }

            var topStoriesArray = await Task.WhenAll(getTopStoryTaskList).ConfigureAwait(false);

            return topStoriesArray.Where(x => x != null).OrderByDescending(x => x.Score).ToList();
        }

        Task<StoryModel> GetStory(string storyId) => GetDataObjectFromAPI<StoryModel>($"https://hacker-news.firebaseio.com/v0/item/{storyId}.json?print=pretty");

        async Task<List<string>> GetTopStoryIDs()
        {
            try
            {
                return await GetDataObjectFromAPI<List<string>>("https://hacker-news.firebaseio.com/v0/topstories.json?print=pretty").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                OnErrorOccurred(e.Message);
                return new List<string>();
            }
        }

        void OnErrorOccurred(string message) => _errorOcurredEventManager?.HandleEvent(this, message, nameof(ErrorOcurred));
        #endregion
    }
}

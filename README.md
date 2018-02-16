# CacheCrow
CacheCrow is a simple key-value, LFU, time-based cache. It is thread safe and lightweight.

### Technicality
CacheCrow internally deploys 2 cache storage to maintain data. This helps maintain the performance of the cache.
The 2 caches are:
  - Active CacheCrow
  - Dormant CacheCrow


#### Active CacheCrow
As the name suggests, it is the currently active cache. It uses in-memory to store data entries. It has a limited size that is user defined. At initilization, the entries in dormant cache, having most lookup hits are loaded in active cache. As the time goes, data entries are expired, removed from active cache and new entries having most lookup hits are loaded in-memory from Dormant CacheCrow.

#### Dormant CacheCrow
Data entries that have least lookup hits resides in dormant cache. All data is stored on disk, in a file '\_crow\CacheCrow'. It has no limit on size and can grow upto total disk size. As data entries are in dormant cache, they cannot expire on their own. For removing expired entries 'Cleaner' is employed that is called every x milli-seconds and its value is only configurable at initialization time.

#### Add Remove data.
Data addition can takes place in 3 conditions:
 - If user is adding new data to CacheCrow, it is first inserted to active cache i.e. if it can accomodate as it has limited size. Else it is inserted into dormant cache.
 - If new space has been accomodated in active cache due to data expiry/removal of data. In this situation, data entry in dormant cache having most lookup hits is loaded into active cache.
 - Auto swapped when key is searched/value is fetched. 
 
Data removal takes place in 3 conditions:
 - If user is removing data from CacheCrow, then corresponding entry is removed from Active or Dormant cache.
 - Data expiry also leads to removal.
 - Auto swapped when key is searched/value is fetched.

####  Auto data swapping between Active and Dormant CacheCrow
When a data entry in dormant cache is looked up for or its value is fetched then LFU is performed, the data entry with least hits/frequency in active cache is compared with data entry with most hits in dormant cache. If the former has less hits then the two entries are swapped between active and dormant cache. Auto data swapping also works while adding data entries to active cache, however for new entries it works as stated above in section [Add Remove data](#add-remove-data).

#### Why there are two layers in CacheCrow ?
Internally CacheCrow relies on Dictionary<> and it takes a down hit in performance when the size increases. To maintain its performance 2 different caches have been deployed, one with a fixed size(Active) having frequently used data and the other with a variable size. 

### Installation via Nuget
Use Visual Studio Powershell command
```sh
$ Install-Package CacheCrow
```
OR
[Download Package](https://www.nuget.org/packages/CacheCrow)

### Available Methods
| Method | Summary |
| ------ | ------ |
| ActiveCount  | Gets number of entries in Active CacheCrow |
| Count  | Gets total number of entries in CacheCrow |
| PreviousCount  | Gets a previously calculated total number of entries in CacheCrow. Note: Should be considered if realtime values are not required. |
|Initialize()|Initializes CacheCrow, creates a directory '_crow' in root if not present.
| Clear() | Removes all entries from CacheCrow, including entries in dormant cache and raises EmptyCacheEvent. |
| Add(K key, V data) | Inputs entry in Active CacheCrow if its size is not exceeded else adds the entry in Dormant CacheCrow. |
| Update(K key, V data); | Searches the key in both Active and Dormant CacheCrow and if found then updates the value |
| LookUp(K key); |  Checks if key is present in CacheCrow(Active+Dormant), Note: LFU maybe performed and entries maybe swapped between Active and Dormant CacheCrow.
|ActiveLookUp(K key);|Checks if key is present in the Active CacheCrow.
|Remove(K key)|Removes the entry from CacheCrow(Active+Dormant) corresponsing to the key
|ActiveRemove(K key)| Removes the entry from Active CacheCrow corresponsing to the key.
|GetValue(K key)|Lookups the key in Active+Dormant CacheCrow, if found then increments the frequency.
|GetActiveValue(K key)|Lookups the key in Active CacheCrow, if found then increments the frequency. Note: LFU maybe performed and entries maybe swapped between Active and Dormant CacheCrow.
|EmptyCacheHandler EmptyCacheEvent|Event is raised when CacheCrow is empty.Note: It is also periodically raised by Cleaner when CacheCrow is empty.

### Code Snippet
```cs
// initialization of singleton class
ICacheCrow<string, string> cache = CacheCrow<string, string>.Initialize(1000);

// adding value to cache
cache.Add("#12","Jack");

// searching value in cache
var flag = cache.LookUp("#12");
if(flag)
{
    Console.WriteLine("Found");
}

// removing value
var value = cache.Remove("#12");

```
### Where can it be used ?
Any small to medium .Net desktop/Web application can make use of CacheCrow for performance boost.

### Need more information ?
Please raise an issue or drop me a message at devileatspie@gmail.com

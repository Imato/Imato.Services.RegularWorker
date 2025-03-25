
drop table workers;

create table if not exists workers 
	(id int not null generated always as identity primary key,
	name varchar(255) not null, 
	host varchar(255) not null, 
	appName varchar(512) not null default '', 
	date timestamp, 
	settings text not null, 
	active bool not null, 
	executed timestamp, 
	hosts int, 
	error text,
		unique (name, host, appName));

drop function public.set_worker(int4, text, text, text, bool, text, timestamptz, text, timestamptz);
  
create or replace function set_worker(
  _id int,
  _name text,
  _appName text,
  _host text,
  _active boolean,
  _settings text,
  _executed timestamp,
  _error text = '',
  _date timestamp = null)
returns table 
  (id int,
  name varchar(255), 
  host varchar(255), 
  appName varchar(512), 
  date timestamp, 
  settings text, 
  active bool, 
  executed timestamp, 
  hosts int, 
  error text,
  activeHosts int)
as 
$$
declare 
  _hosts int = 0;
  _activeHosts int = 0;
begin 
 
  select count(1), sum(iif(w.active, 1, 0))
    into _hosts, _activeHosts
    from workers w
    where w.name = _name 
      and w.appName = _appName
      and w.date >= now() - (60 * interval'1 second')
      and w.host != _host;

  _hosts := _hosts + 1;
  _activeHosts := coalesce(_activeHosts, 0);
  _date := coalesce(_date, current_timestamp);

  update workers w
    set active = _active, 
      date = _date,
      hosts = _hosts,
      executed = _executed,
      error = _error
    where (_id > 0 and w.id = _id) or (w.host = _host and w.name = _name and w.appName = _appName)
    returning w.id
    into _id;
  
  if (_id is null or _id = 0)
    then 
    insert into workers
      (name, host, appName, date, settings, active, hosts, error, executed)
    values 
      (_name, _host, _appName, _date, _settings, _active, _hosts, _error, _executed); 
  end if;
    
 return query
 (select w.id, w.name, w.host, w.appName, w.date,
 w.settings, w.active, w.executed, w.hosts, w.error, _activeHosts as activeHosts
    from workers w
    where (_id > 0 and w.id = _id) or (w.host = _host and w.name = _name and w.appName = _appName)
    limit 1);
  
end
$$ language plpgsql;
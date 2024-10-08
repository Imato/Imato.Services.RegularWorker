﻿if object_id('dbo.Workers') is null 
begin 
	create table dbo.Workers 
		(id int not null identity(1, 1), 
		name varchar(255) not null, 
		host varchar(255) not null, 
		appName varchar(512) not null default '', 
		date datetime not null, 
		settings varchar(2000) not null, 
		active bit not null, 
		hosts int); 
	alter table dbo.Workers 
		add constraint Workers__PK primary key (id); 
	alter table dbo.Workers 
		add constraint Workers__UK unique (name, host, appName); 
end
go

alter table dbo.Workers 
	add executed datetime, hosts int;
go

create proc dbo.SetWorker
	@id int,
	@name varchar(255),
	@appName varchar(512),
	@active bit,
	@host varchar(255),
	@settings varchar(2000)
as 
begin 
	set nocount on;

	declare @hosts int = 0,
		@activeHosts int = 0;

	begin transaction; 

	select @hosts = count(1), @activeHosts = sum(iif(active = 1, 1, 0))
		from dbo.Workers 
		where name = @name 
			and date >= dateadd(millisecond, -60000, getdate())
			and @host != host;

	set @hosts = @hosts + 1;
	set @activeHosts = isnull(@activeHosts, 0);

	update dbo.Workers 
		set date = getdate(), 
			active = @active,
			hosts = @hosts
		where (@id > 0 and id = @id) 
			or (host = @host 
				and name = @name 
				and appName = @appName); 
				
	if @@ROWCOUNT = 0 
		insert into dbo.Workers 
			(name, host, appName, date, settings, active, hosts) 
		values 
			(@name, @host, @appName, getdate(), @settings, @active, @hosts); 
			
	select top 1 *, @activeHosts as activeHosts
		from dbo.Workers 
		where (@id > 0 and id = @id) 
			or (host = @host 
				and name = @name 
				and appName = @appName); 

	commit transaction;
end;
go
